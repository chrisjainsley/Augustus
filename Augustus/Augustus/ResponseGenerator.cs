using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Augustus
{
    internal class ResponseGenerator
    {
        private readonly OpenAIClient openAiClient;
        private readonly OpenAIRequestHandler requestHandler;
        private readonly APISimulatorOptions options;
        private readonly APISimulator.FileManager fileManager;
        private readonly InstructionsContainer instructionsContainer;
        private readonly ConcurrentDictionary<int, Task> _pendingCacheWrites = new();
        private int _cacheWriteId;

        public ResponseGenerator(APISimulatorOptions options, InstructionsContainer instructionsContainer, APISimulator.FileManager fileManager)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.instructionsContainer = instructionsContainer ?? throw new ArgumentNullException(nameof(instructionsContainer));
            this.fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));

            // Validation is now done in APISimulator constructor (fail-fast)
            // Create appropriate client based on configuration
            if (options.UseAzureOpenAI && !string.IsNullOrEmpty(options.OpenAIEndpoint))
            {
                // Use Azure OpenAI client
                openAiClient = new AzureOpenAIClient(
                    new Uri(options.OpenAIEndpoint),
                    new System.ClientModel.ApiKeyCredential(options.OpenAIApiKey));
            }
            else
            {
                // Use standard OpenAI client
                openAiClient = new OpenAIClient(options.OpenAIApiKey);
            }
            requestHandler = new OpenAIRequestHandler(openAiClient, options);
        }

        public async Task GenerateResponse(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            try
            {
                var curlRequest = await httpContext.Request.ToCurlCommandAsync().ConfigureAwait(false);

                // Get route-specific instructions based on the request path and method
                // (must happen before hash so route instructions are included in cache key)
                var instructions = instructionsContainer.GetInstructionsForRequest(
                    httpContext.Request.Path.Value ?? "/",
                    httpContext.Request.Method);

                var requestHash = GenerateRequestHash(curlRequest, instructions);

                // Try to get cached response first
                if (options.EnableCaching)
                {
                    var cachedResponse = await fileManager.ReadCachedResponseAsync(requestHash).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(cachedResponse))
                    {
                        httpContext.Response.ContentType = "application/json";
                        await httpContext.Response.WriteAsync(cachedResponse, cancellationToken);
                        return;
                    }
                }

                if (!instructions.Any())
                {
                    await WriteErrorResponse(httpContext, "No instructions provided. Please add instructions using AddInstruction().", 500, cancellationToken);
                    return;
                }

                // Create system messages from the instructions
                List<ChatMessage> messages = new List<ChatMessage>();
                foreach (var instruction in instructions)
                {
                    messages.Add(ChatMessage.CreateSystemMessage(instruction));
                }

                // Add the curlRequest as the user message
                messages.Add(ChatMessage.CreateUserMessage(curlRequest));

                // Force the simulation LLM to return a valid JSON object.
                // The simulation LLM generates the Azure OpenAI API *response body*, which must always be valid JSON.
                // It does not use tool_calls itself — it generates a JSON body that *contains* tool_calls when appropriate.
                // json_object mode ensures the response is parseable by the Azure OpenAI client SDK.
                var chatOptions = new ChatCompletionOptions
                {
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                };

                // Send chat messages to OpenAI with retry and backoff logic
                var chatResults = await requestHandler.CompleteChatWithRetryAsync(messages, chatOptions, cancellationToken).ConfigureAwait(false);

                if (chatResults?.Value?.Content == null || chatResults.Value.Content.Count == 0)
                {
                    await WriteErrorResponse(httpContext, "No response generated from OpenAI", 500, cancellationToken);
                    return;
                }

                // Get the first result and validate it's not null
                var firstContent = chatResults.Value.Content[0];
                if (firstContent == null || string.IsNullOrEmpty(firstContent.Text))
                {
                    await WriteErrorResponse(httpContext, "Empty or null text content from OpenAI", 500, cancellationToken);
                    return;
                }

                // Strip any markdown code fences the AI may have added despite instructions
                var responseContent = StripMarkdown(firstContent.Text);

                // Validate and sanitize the response to ensure it matches the expected format
                responseContent = ValidateAndSanitizeResponse(responseContent);

                // Send the response to the client immediately, before caching
                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(responseContent, cancellationToken);

                // Cache the response after sending — avoids blocking the caller on file I/O.
                // Tasks are tracked so they can be drained on shutdown via DrainPendingCacheWritesAsync().
                if (options.EnableCaching)
                {
                    var id = Interlocked.Increment(ref _cacheWriteId);
                    var cacheTask = CacheInBackgroundAsync(id, requestHash, responseContent, curlRequest, instructions);
                    _pendingCacheWrites.TryAdd(id, cacheTask);
                }
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                await WriteErrorResponse(httpContext, $"Internal error: {ex.Message}", 500, cancellationToken);
            }
        }

        private async Task CacheInBackgroundAsync(int id, string requestHash, string responseContent, string curlRequest, List<string> instructions)
        {
            try
            {
                await fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
            }
            finally
            {
                _pendingCacheWrites.TryRemove(id, out _);
            }
        }

        /// <summary>
        /// Waits for all in-flight background cache writes to complete.
        /// Called during shutdown to prevent cache files from being lost.
        /// </summary>
        public async Task DrainPendingCacheWritesAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _pendingCacheWrites.Values.ToArray();

            if (tasks.Length > 0)
            {
                var allTasks = Task.WhenAll(tasks);
                var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
                await Task.WhenAny(allTasks, cancellationTask).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        private static string StripMarkdown(string text)
        {
            var trimmed = text.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(7);
            else if (trimmed.StartsWith("```"))
                trimmed = trimmed.Substring(3);

            if (trimmed.EndsWith("```"))
                trimmed = trimmed.Substring(0, trimmed.Length - 3);

            return trimmed.Trim();
        }

        /// <summary>
        /// Validates and sanitizes the AI-generated response to ensure it matches OpenAI SDK requirements.
        /// This method catches common formatting errors that would cause deserialization failures.
        /// </summary>
        private string ValidateAndSanitizeResponse(string responseJson)
        {
            try
            {
                // Parse the JSON to validate structure
                using var document = JsonDocument.Parse(responseJson);
                var root = document.RootElement;

                // Check if this is a chat completion response (has "choices" array)
                if (root.TryGetProperty("choices", out var choicesElement) && choicesElement.ValueKind == JsonValueKind.Array)
                {
                    // Validate each choice has proper structure
                    foreach (var choice in choicesElement.EnumerateArray())
                    {
                        // Ensure "message" is an object, not a string
                        if (choice.TryGetProperty("message", out var messageElement))
                        {
                            if (messageElement.ValueKind == JsonValueKind.String)
                            {
                                Console.WriteLine("Warning: 'message' field is a string instead of an object. This will cause deserialization errors.");
                                throw new JsonException("Invalid response format: 'message' must be an object, not a string");
                            }
                        }

                        // Ensure "content_filter_results" (if present) is an object, not a string
                        if (choice.TryGetProperty("content_filter_results", out var filterElement))
                        {
                            if (filterElement.ValueKind == JsonValueKind.String)
                            {
                                Console.WriteLine("Warning: 'content_filter_results' field is a string instead of an object. This will cause deserialization errors.");
                                throw new JsonException("Invalid response format: 'content_filter_results' must be an object, not a string");
                            }
                        }
                    }
                }

                // If validation passed, return original JSON
                return responseJson;
            }
            catch (JsonException ex)
            {
                // Log the error with the problematic JSON for debugging
                Console.WriteLine($"JSON Validation Error: {ex.Message}");
                Console.WriteLine($"Problematic JSON (first 500 chars): {responseJson.Substring(0, Math.Min(500, responseJson.Length))}");

                // Re-throw the exception - we don't want to send invalid JSON to clients
                throw new InvalidOperationException($"The AI generated an invalid response format that would fail SDK deserialization: {ex.Message}. Please regenerate the response.", ex);
            }
        }

        private async Task WriteErrorResponse(HttpContext context, string message, int statusCode, CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var errorResponse = JsonSerializer.Serialize(new { error = message, status = statusCode });
            await context.Response.WriteAsync(errorResponse, cancellationToken);
        }

        private string GenerateRequestHash(string curlRequest, List<string> instructions)
        {
            var combinedContent = string.Join("|", instructions) + "|" + curlRequest;
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combinedContent));
            // Use full hash to prevent collisions (64 hex characters from SHA256)
            // File systems can handle long names, and cache correctness is more important than brevity
            return Convert.ToHexString(hash);
        }
    }
}
