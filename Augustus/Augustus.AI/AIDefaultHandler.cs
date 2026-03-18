using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Augustus.AI;

/// <summary>
/// An <see cref="IRequestHandler"/> that uses OpenAI to generate API responses.
/// Installed as the default handler on the simulator's routing pipeline.
/// </summary>
internal class AIDefaultHandler : IRequestHandler
{
    private readonly OpenAIClient? openAiClient;
    private readonly OpenAIRequestHandler? requestHandler;
    private readonly bool cacheOnly;
    private readonly APISimulator simulator;
    private readonly BackgroundCacheWriter _cacheWriter = new();

    public AIDefaultHandler(APISimulator simulator, AIOptions aiOptions)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.cacheOnly = simulator.Options.CacheOnly;

        if (!cacheOnly)
        {
            if (aiOptions.UseAzureOpenAI)
            {
                openAiClient = new AzureOpenAIClient(
                    new Uri(aiOptions.OpenAIEndpoint),
                    new System.ClientModel.ApiKeyCredential(aiOptions.OpenAIApiKey));
            }
            else
            {
                openAiClient = new OpenAIClient(aiOptions.OpenAIApiKey);
            }
            requestHandler = new OpenAIRequestHandler(openAiClient, aiOptions);
        }
    }

    public async Task HandleAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var fileManager = simulator.CacheFileManager;
        var instructionsContainer = simulator.InstructionsContainer;
        var options = simulator.Options;

        try
        {
            var bodyBytes = await httpContext.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);

            var curlRequest = await Augustus.HttpRequestExtensions.ToCurlCommandAsync(httpContext.Request).ConfigureAwait(false);

            var instructions = instructionsContainer.GetInstructionsForRequest(
                httpContext.Request.Path.Value ?? "/",
                httpContext.Request.Method);

            var requestHash = CacheKeyComputer.ComputeCacheKey(
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? "/",
                httpContext.Request.QueryString.Value,
                bodyBytes,
                instructions);

            if (options.EnableCaching)
            {
                var cachedResponse = await fileManager.ReadCachedResponseAsync(requestHash).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    cachedResponse = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(
                        cachedResponse, httpContext.Request.Path.Value ?? "/");
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                    return;
                }
            }

            if (cacheOnly)
            {
                await WriteErrorResponse(httpContext,
                    $"Cache-only mode: no cached response found for request hash '{requestHash}'. " +
                    "Run tests locally with an OpenAI API key to generate and cache this response.",
                    503, cancellationToken);
                return;
            }

            if (requestHandler is null)
            {
                await WriteErrorResponse(httpContext, "OpenAI client is not initialized. Provide an API key or enable cache-only mode.", 500, cancellationToken);
                return;
            }

            if (!instructions.Any())
            {
                await WriteErrorResponse(httpContext, "No instructions provided. Please add instructions using AddInstruction().", 500, cancellationToken);
                return;
            }

            List<ChatMessage> messages = new List<ChatMessage>();
            foreach (var instruction in instructions)
            {
                messages.Add(ChatMessage.CreateSystemMessage(instruction));
            }
            messages.Add(ChatMessage.CreateUserMessage(curlRequest));

            var chatOptions = new ChatCompletionOptions
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var chatResults = await requestHandler.CompleteChatWithRetryAsync(messages, chatOptions, cancellationToken).ConfigureAwait(false);

            if (chatResults?.Value?.Content == null || chatResults.Value.Content.Count == 0)
            {
                await WriteErrorResponse(httpContext, "No response generated from OpenAI", 500, cancellationToken);
                return;
            }

            var firstContent = chatResults.Value.Content[0];
            if (firstContent == null || string.IsNullOrEmpty(firstContent.Text))
            {
                await WriteErrorResponse(httpContext, "Empty or null text content from OpenAI", 500, cancellationToken);
                return;
            }

            var responseContent = StripMarkdown(firstContent.Text);

            responseContent = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(
                responseContent, httpContext.Request.Path.Value ?? "/");

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken);

            if (options.EnableCaching)
            {
                _cacheWriter.Enqueue(() => fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions));
            }
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            await WriteErrorResponse(httpContext, $"Internal error: {ex.Message}", 500, cancellationToken);
        }
    }

    public Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default)
        => _cacheWriter.DrainAsync(cancellationToken);

    private static string StripMarkdown(string text)
    {
        var trimmed = text.Trim();
        const string jsonFence = "```json";
        const string fence = "```";

        if (trimmed.StartsWith(jsonFence, StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[jsonFence.Length..];
        else if (trimmed.StartsWith(fence))
            trimmed = trimmed[fence.Length..];

        if (trimmed.EndsWith(fence))
            trimmed = trimmed[..^fence.Length];

        return trimmed.Trim();
    }

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message, status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }
}
