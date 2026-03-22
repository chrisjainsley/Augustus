namespace Augustus.AI;

using Augustus;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

/// <summary>
/// Response strategy that uses OpenAI (or Azure OpenAI) to generate realistic API responses for a matched route.
/// Uses the same cache key algorithm and on-disk cache as <see cref="AIDefaultHandler"/>.
/// </summary>
public sealed class AIResponseStrategy : IResponseStrategy, IDisposable
{
    private readonly APISimulator simulator;
    private readonly bool ownsDetachedSimulator;
    private readonly AIOptions options;
    private readonly List<string> instructions;
    private readonly IReadOnlyCollection<string> mergedDynamicFields;
    private readonly OpenAIClient openAiClient;
    private readonly OpenAIRequestHandler requestHandler;

    /// <summary>
    /// Standalone constructor using <see cref="AIOptions.CacheFolderPath"/> for disk cache (no shared <see cref="APISimulator"/>).
    /// Prefer <see cref="RouteBuilderExtensions.UseAI"/> so cache keys and dynamic fields match the simulator.
    /// </summary>
    public AIResponseStrategy(AIOptions options, params string[] instructions)
        : this(
            CreateDetachedSimulator(options),
            options,
            instructions ?? Array.Empty<string>(),
            Array.Empty<string>(),
            ownsDetachedSimulator: true)
    {
    }

    internal AIResponseStrategy(
        APISimulator simulator,
        AIOptions options,
        string[] instructions,
        IReadOnlyCollection<string> mergedDynamicFields)
        : this(simulator, options, instructions, mergedDynamicFields, ownsDetachedSimulator: false)
    {
    }

    private AIResponseStrategy(
        APISimulator simulator,
        AIOptions options,
        string[] instructions,
        IReadOnlyCollection<string> mergedDynamicFields,
        bool ownsDetachedSimulator)
    {
        this.simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        this.ownsDetachedSimulator = ownsDetachedSimulator;
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.instructions = new List<string>(instructions ?? Array.Empty<string>());
        this.mergedDynamicFields = mergedDynamicFields ?? Array.Empty<string>();

        options.Validate();

        if (options.UseAzureOpenAI)
        {
            openAiClient = new AzureOpenAIClient(
                new Uri(options.OpenAIEndpoint),
                new System.ClientModel.ApiKeyCredential(options.OpenAIApiKey));
        }
        else
        {
            openAiClient = new OpenAIClient(options.OpenAIApiKey);
        }

        requestHandler = new OpenAIRequestHandler(openAiClient, options);
    }

    private static APISimulator CreateDetachedSimulator(AIOptions aiOptions)
    {
        var simOptions = new APISimulatorOptions
        {
            CacheFolderPath = aiOptions.CacheFolderPath,
            EnableCaching = aiOptions.EnableCaching,
            Port = 0,
            AutoRemoveStaleCache = false
        };
        return new APISimulator("DetachedAI", simOptions);
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var fileManager = simulator.CacheFileManager;
        var simOptions = simulator.Options;

        try
        {
            var bodyBytes = await httpContext.Request.ReadBodyBytesAsync(cancellationToken).ConfigureAwait(false);
            var curlRequest = await httpContext.Request.ToCurlCommandAsync().ConfigureAwait(false);

            var path = httpContext.Request.Path.Value ?? "/";
            var requestHash = CacheKeyComputer.ComputeCacheKey(
                httpContext.Request.Method,
                path,
                httpContext.Request.QueryString.Value,
                bodyBytes,
                out _,
                instructions,
                mergedDynamicFields);

            if (simOptions.EnableCaching && options.EnableCaching)
            {
                var cachedResponse = await fileManager.ReadCachedResponseAsync(requestHash).ConfigureAwait(false);
                if (string.IsNullOrEmpty(cachedResponse))
                {
                    var legacyHash = CacheManager.GenerateLegacyCurlBasedCacheKey(curlRequest, instructions);
                    cachedResponse = await fileManager.ReadCachedResponseAsync(legacyHash).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    cachedResponse = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(cachedResponse, path);
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }

            List<ChatMessage> messages = new();
            foreach (var instruction in instructions)
            {
                messages.Add(ChatMessage.CreateSystemMessage(instruction));
            }

            messages.Add(ChatMessage.CreateUserMessage(curlRequest));

            var chatOptions = AIResponseFormatting.CreateJsonObjectChatOptions();
            var chatResults = await requestHandler
                .CompleteChatWithRetryAsync(requestHash, messages, chatOptions, cancellationToken)
                .ConfigureAwait(false);

            if (chatResults?.Value?.Content == null || chatResults.Value.Content.Count == 0)
            {
                await WriteErrorResponse(httpContext, "No response generated from OpenAI", 500, cancellationToken).ConfigureAwait(false);
                return;
            }

            var firstContent = chatResults.Value.Content[0];
            if (firstContent == null || string.IsNullOrEmpty(firstContent.Text))
            {
                await WriteErrorResponse(httpContext, "Empty or null text content from OpenAI", 500, cancellationToken).ConfigureAwait(false);
                return;
            }

            var responseContent = AIResponseFormatting.StripMarkdownFences(firstContent.Text);
            responseContent = ChatCompletionResponseNormalizer.NormalizeIfChatCompletion(responseContent, path);

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken).ConfigureAwait(false);

            if (simOptions.EnableCaching && options.EnableCaching)
            {
                try
                {
                    await fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
                }
            }
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request failed: {ex}");
            await WriteErrorResponse(httpContext, "Failed to generate response from OpenAI API", 502, cancellationToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request timeout: {ex}");
            await WriteErrorResponse(httpContext, "Request timeout while contacting OpenAI API", 504, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex is not TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"Operation cancelled: {ex}");
            await WriteErrorResponse(httpContext, "Request cancelled", 499, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error generating response: {ex}");
            await WriteErrorResponse(httpContext, "Internal server error", 500, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message ?? "Unknown error", status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        requestHandler.Dispose();
        if (ownsDetachedSimulator)
        {
            simulator.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
