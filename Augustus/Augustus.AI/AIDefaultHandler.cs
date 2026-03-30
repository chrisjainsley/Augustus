using Augustus;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
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
    private readonly AIOptions aiOptions;
    private readonly BackgroundCacheWriter _cacheWriter = new();

    public AIDefaultHandler(APISimulator simulator, AIOptions aiOptions)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.aiOptions = aiOptions ?? throw new ArgumentNullException(nameof(aiOptions));
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

            var instructions = instructionsContainer.GetInstructionsForRequest(
                httpContext.Request.Path.Value ?? "/",
                httpContext.Request.Method);

            var requestHash = CacheKeyComputer.ComputeCacheKey(
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? "/",
                httpContext.Request.QueryString.Value,
                bodyBytes,
                out var materializedBody,
                instructions,
                options.DynamicContentFields);

            if (options.EnableCaching)
            {
                var cachedEntry = await fileManager.ReadCachedEntryAsync(requestHash).ConfigureAwait(false);
                if (cachedEntry?.Response is { Length: > 0 } cachedResponse)
                {
                    if (!cachedEntry.Normalized)
                    {
                        cachedResponse = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(
                            cachedResponse, httpContext.Request.Path.Value ?? "/");
                    }
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                    return;
                }
            }

            if (cacheOnly)
            {
                var message =
                    $"Cache-only mode: no cached response found for request hash '{requestHash}'. " +
                    "Run tests locally with an OpenAI API key to generate and cache this response.";
                if (aiOptions.CacheMissMaterializedBodyPrefixSha256ByteCount > 0)
                {
                    var n = Math.Min(aiOptions.CacheMissMaterializedBodyPrefixSha256ByteCount, materializedBody.Length);
                    if (n > 0)
                    {
                        var digest = SHA256.HashData(materializedBody.AsSpan(0, n));
                        message += $" Materialized body prefix SHA-256 (first {n} bytes): {Convert.ToHexString(digest)}.";
                    }
                }

                await WriteErrorResponse(httpContext, message, 503, cancellationToken);
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

            // Defer curl command generation until after cache check — avoids re-reading the body stream,
            // iterating headers, and string building on cache hits. Filter noisy headers to reduce
            // OpenAI prompt token usage.
            var curlRequest = await Augustus.HttpRequestExtensions.ToCurlCommandAsync(
                httpContext.Request, HttpRequestExtensions.DefaultAISkipHeaders).ConfigureAwait(false);
            // Sanitize any residual sensitive values (query params, body tokens) before forwarding to OpenAI.
            curlRequest = SensitiveDataSanitizer.SanitizeSensitiveValues(curlRequest);

            List<ChatMessage> messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage(string.Join("\n\n", instructions)),
                ChatMessage.CreateUserMessage(curlRequest)
            };

            var chatOptions = AIResponseFormatting.CreateJsonObjectChatOptions();

            var chatResults = await requestHandler
                .CompleteChatWithRetryAsync(requestHash, messages, chatOptions, cancellationToken)
                .ConfigureAwait(false);

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

            var responseContent = AIResponseFormatting.StripMarkdownFences(firstContent.Text);

            responseContent = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(
                responseContent, httpContext.Request.Path.Value ?? "/");

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken);

            if (options.EnableCaching)
            {
                _cacheWriter.Enqueue(() => fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions, normalized: true));
            }
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            await WriteErrorResponse(httpContext, $"Internal error: {ex.Message}", 500, cancellationToken);
        }
    }

    public Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default)
        => _cacheWriter.DrainAsync(cancellationToken);

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken = default)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message, status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }
}
