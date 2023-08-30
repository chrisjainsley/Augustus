using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http;

namespace Augustus
{
    internal class ResponseGenerator
    {
        private readonly OpenAIClient openAiClient;

        public ResponseGenerator()
        {
            openAiClient = new OpenAIClient("");
        }

        public async Task GenerateResponse(HttpContext httpContext)
        {
            var curlRequest = await httpContext.Request.ToCurlCommandAsync();
            var instructions = InstructionsContainer.Instructions;

            // Create system messages from the instructions
            List<ChatMessage> messages = instructions.Select(instruction => new ChatMessage
            {
                Role = "system",
                Content = instruction
            }).ToList();

            // Add the curlRequest as the user message
            messages.Add(new ChatMessage
            {
                Role = "user",
                Content = curlRequest
            });

            // Send chat messages to the openAiClient
            var chatResults = await openAiClient.GetChatCompletionsAsync("model", new ChatCompletionsOptions(messages));

            // Get the first result and set a variable
            var firstResult = chatResults.Value.Choices.First();           

            await httpContext.Response.WriteAsync(firstResult.Message.Content);
        }        
    }
}
