using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat;
using Microsoft.AspNetCore.Http;
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

        public ResponseGenerator(APISimulatorOptions options, InstructionsContainer instructionsContainer)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.instructionsContainer = instructionsContainer ?? throw new ArgumentNullException(nameof(instructionsContainer));

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

            fileManager = new APISimulator.FileManager(options.CacheFolderPath);
        }

        public async Task GenerateResponse(HttpContext httpContext, CancellationToken cancellationToken = default)
        {
            try
            {
                var curlRequest = await httpContext.Request.ToCurlCommandAsync();

                // Get route-specific instructions based on the request path and method
                // (must happen before hash so route instructions are included in cache key)
                var instructions = instructionsContainer.GetInstructionsForRequest(
                    httpContext.Request.Path.Value ?? "/",
                    httpContext.Request.Method);

                var requestHash = GenerateRequestHash(curlRequest, instructions);

                // Try to get cached response first
                if (options.EnableCaching)
                {
                    var cachedResponse = await fileManager.ReadCachedResponseAsync(requestHash);
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
                var chatResults = await requestHandler.CompleteChatWithRetryAsync(messages, chatOptions, cancellationToken);

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

                // Cache the response if caching is enabled
                if (options.EnableCaching)
                {
                    try
                    {
                        await fileManager.CacheResponseAsync(requestHash, responseContent, curlRequest, instructions);
                    }
                    catch (Exception ex)
                    {
                        // Log caching error but don't fail the request
                        Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
                    }
                }

                httpContext.Response.ContentType = "application/json";
                await httpContext.Response.WriteAsync(responseContent, cancellationToken);
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
            {
                await WriteErrorResponse(httpContext, $"Internal error: {ex.Message}", 500, cancellationToken);
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
