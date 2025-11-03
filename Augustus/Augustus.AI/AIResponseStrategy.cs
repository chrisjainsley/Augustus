namespace Augustus.AI;

using Augustus;
using Microsoft.AspNetCore.Http;
using OpenAI;
using OpenAI.Chat;
using System.Text.Json;

/// <summary>
/// Response strategy that uses OpenAI to generate realistic API responses.
/// </summary>
public class AIResponseStrategy : IResponseStrategy, IDisposable
{
    private readonly AIOptions options;
    private readonly List<string> instructions;
    private readonly OpenAIClient openAiClient;
    private readonly OpenAIRequestHandler requestHandler;
    private readonly CacheManager? cacheManager;

    public AIResponseStrategy(AIOptions options, params string[] instructions)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.instructions = new List<string>(instructions ?? Array.Empty<string>());

        options.Validate();

        openAiClient = new OpenAIClient(options.OpenAIApiKey);
        requestHandler = new OpenAIRequestHandler(openAiClient, options);

        if (options.EnableCaching)
        {
            cacheManager = new CacheManager(options.CacheFolderPath);
        }
    }

    public async Task GenerateResponseAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var curlRequest = await httpContext.Request.ToCurlCommandAsync();
            var requestHash = CacheManager.GenerateRequestHash(curlRequest, instructions);

            // Try cache first
            if (cacheManager != null)
            {
                var cachedResponse = await cacheManager.ReadCachedResponseAsync(requestHash);
                if (!string.IsNullOrEmpty(cachedResponse))
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                    return;
                }
            }

            // Build chat messages
            List<ChatMessage> messages = new();
            foreach (var instruction in instructions)
            {
                messages.Add(ChatMessage.CreateSystemMessage(instruction));
            }
            messages.Add(ChatMessage.CreateUserMessage(curlRequest));

            // Call OpenAI
            var chatResults = await requestHandler.CompleteChatWithRetryAsync(messages, cancellationToken);

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

            var responseContent = firstContent.Text;

            // Cache the response
            if (cacheManager != null)
            {
                try
                {
                    await cacheManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
                }
            }

            httpContext.Response.ContentType = "application/json";
            await httpContext.Response.WriteAsync(responseContent, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request failed: {ex}");
            await WriteErrorResponse(httpContext, "Failed to generate response from OpenAI API", 502, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine($"OpenAI request timeout: {ex}");
            await WriteErrorResponse(httpContext, "Request timeout while contacting OpenAI API", 504, cancellationToken);
        }
        catch (OperationCanceledException ex) when (!(ex is TaskCanceledException))
        {
            System.Diagnostics.Debug.WriteLine($"Operation cancelled: {ex}");
            await WriteErrorResponse(httpContext, "Request cancelled", 499, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error generating response: {ex}");
            await WriteErrorResponse(httpContext, "Internal server error", 500, cancellationToken);
        }
    }

    private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var errorResponse = JsonSerializer.Serialize(new { error = message ?? "Unknown error", status = statusCode });
        await context.Response.WriteAsync(errorResponse, cancellationToken);
    }

    /// <summary>
    /// Disposes the AI response strategy and its resources.
    /// </summary>
    public void Dispose()
    {
        requestHandler?.Dispose();
    }
}
