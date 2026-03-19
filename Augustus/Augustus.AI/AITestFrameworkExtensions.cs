namespace Augustus.AI;

using Augustus;
using Augustus.Extensions;
using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for creating AI-powered API simulators in test classes.
/// </summary>
/// <remarks>
/// These are the AI-dependent factory methods moved from Augustus core.
/// For basic (non-AI) simulator creation, see <see cref="TestFrameworkExtensions"/> in Augustus core.
/// </remarks>
public static class AITestFrameworkExtensions
{
    /// <summary>
    /// Creates a new OpenAI pass-through proxy that forwards requests to the real OpenAI API and caches responses.
    /// </summary>
    public static APISimulator CreateOpenAIProxy(this object testClass, Action<AIOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var aiOptions = new AIOptions();
        configure?.Invoke(aiOptions);

        var simulator = testClass.CreateAPISimulator("OpenAI Proxy", callerFilePath: callerFilePath);
        simulator.UseProxy(aiOptions, aiOptions.OpenAIEndpoint.Length > 0 ? aiOptions.OpenAIEndpoint : "https://api.openai.com");
        return simulator;
    }

    /// <summary>
    /// Creates a new Azure OpenAI pass-through proxy that forwards requests to the real Azure OpenAI API and caches responses.
    /// </summary>
    public static APISimulator CreateAzureOpenAIProxy(this object testClass, Action<AIOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var aiOptions = new AIOptions { UseAzureOpenAI = true };
        configure?.Invoke(aiOptions);

        var simulator = testClass.CreateAPISimulator("Azure OpenAI Proxy", callerFilePath: callerFilePath);

        if (!string.IsNullOrEmpty(aiOptions.OpenAIEndpoint))
        {
            simulator.UseProxy(aiOptions, aiOptions.OpenAIEndpoint);
        }
        return simulator;
    }

    /// <summary>
    /// Creates a new OpenAI API simulator with pre-configured context and default instructions.
    /// </summary>
    public static APISimulator CreateOpenAISimulator(this object testClass, Action<AIOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var aiOptions = new AIOptions();
        configure?.Invoke(aiOptions);

        var simulator = testClass.CreateAPISimulator("OpenAI", callerFilePath: callerFilePath);
        simulator.UseAI(aiOptions);

        // Add default instructions for OpenAI API format
        simulator.AddInstruction("CRITICAL: Return ONLY raw JSON. No markdown code fences, no ``` characters, no explanations. Your entire response must be valid, parseable JSON with no surrounding text.");

        simulator.AddInstruction("CRITICAL TYPE REQUIREMENTS: In the JSON response, 'choices' must be a JSON array (not a string), each 'message' must be a JSON object with 'role' and 'content' keys (not a string), 'usage' must be a JSON object (not a string). All numeric fields (index, created, prompt_tokens, completion_tokens, total_tokens) must be JSON numbers, not strings wrapped in quotes.");

        simulator.AddInstruction("Return all responses in valid JSON format matching the official OpenAI API specification.");

        simulator.AddInstruction(@"For chat completion requests (POST /v1/chat/completions), return responses in this format:
{
  ""id"": ""chatcmpl-[random-id]"",
  ""object"": ""chat.completion"",
  ""created"": [unix-timestamp],
  ""model"": ""[model-name-from-request]"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": {
        ""role"": ""assistant"",
        ""content"": ""[generated response content]""
      },
      ""logprobs"": null,
      ""finish_reason"": ""stop""
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  },
  ""system_fingerprint"": ""fp_[random-string]""
}");

        simulator.AddInstruction(@"For completion requests (POST /v1/completions), return responses in this format:
{
  ""id"": ""cmpl-[random-id]"",
  ""object"": ""text_completion"",
  ""created"": [unix-timestamp],
  ""model"": ""[model-name-from-request]"",
  ""choices"": [
    {
      ""text"": ""[generated completion text]"",
      ""index"": 0,
      ""finish_reason"": ""stop""
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  }
}");

        simulator.AddInstruction(@"For model list requests (GET /v1/models), return responses in this format:
{
  ""object"": ""list"",
  ""data"": [
    {
      ""id"": ""gpt-4"",
      ""object"": ""model"",
      ""created"": [unix-timestamp],
      ""owned_by"": ""openai""
    },
    {
      ""id"": ""gpt-3.5-turbo"",
      ""object"": ""model"",
      ""created"": [unix-timestamp],
      ""owned_by"": ""openai""
    }
  ]
}");

        simulator.AddInstruction(@"For embedding requests (POST /v1/embeddings), return responses in this format:
{
  ""object"": ""list"",
  ""data"": [
    {
      ""object"": ""embedding"",
      ""embedding"": [[random array of floats with 1536 dimensions for ada-002]],
      ""index"": 0
    }
  ],
  ""model"": ""[model-name-from-request]"",
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""total_tokens"": [realistic-number]
  }
}");

        simulator.AddInstruction(@"For error responses, return in this format:
{
  ""error"": {
    ""message"": ""[error description]"",
    ""type"": ""invalid_request_error"",
    ""param"": null,
    ""code"": ""[error-code]""
  }
}");

        simulator.AddInstruction("Generate realistic IDs using the pattern: chatcmpl-[8-character-random-string] for chat completions, cmpl-[8-character-random-string] for completions.");

        simulator.AddInstruction("Use realistic Unix timestamps for the 'created' field (current time).");

        simulator.AddInstruction("Calculate realistic token counts in the 'usage' field based on the approximate length of input and output text.");

        return simulator;
    }

    /// <summary>
    /// Creates a new Azure OpenAI API simulator with pre-configured context and default instructions.
    /// </summary>
    public static APISimulator CreateAzureOpenAISimulator(this object testClass, Action<AIOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var aiOptions = new AIOptions { UseAzureOpenAI = true };
        configure?.Invoke(aiOptions);

        var simulator = testClass.CreateAPISimulator("Azure OpenAI", callerFilePath: callerFilePath);
        simulator.UseAI(aiOptions);

        // Add default instructions for Azure OpenAI API format
        simulator.AddInstruction("CRITICAL: Return ONLY raw JSON. No markdown code fences, no ``` characters, no explanations. Your entire response must be valid, parseable JSON with no surrounding text.");

        simulator.AddInstruction("CRITICAL TYPE REQUIREMENTS: In the JSON response, 'choices' must be a JSON array (not a string), each 'message' must be a JSON object with 'role' and 'content' keys (not a string), 'usage' must be a JSON object (not a string), 'content_filter_results' must be a JSON object (not a string). All numeric fields (index, created, prompt_tokens, completion_tokens, total_tokens) must be JSON numbers, not strings wrapped in quotes.");

        simulator.AddInstruction("Return all responses in valid JSON format matching the official Azure OpenAI API specification.");

        simulator.AddInstruction("Recognize and handle Azure OpenAI URL patterns: /openai/deployments/{deployment-name}/chat/completions with api-version query parameter.");

        simulator.AddInstruction("Accept authentication via 'api-key' header (Azure style) in addition to 'Authorization: Bearer' header (OpenAI style).");

        simulator.AddInstruction(@"For Azure OpenAI chat completion requests (POST /openai/deployments/{deployment}/chat/completions), examine the request body CAREFULLY to determine which of the 3 response types to return:

CASE 1 — TOOL RESULT ALREADY IN CONVERSATION (HIGHEST PRIORITY):
If the messages array contains ANY message with ""role"": ""tool"" (a function result), the function has ALREADY been executed successfully. You MUST return a stop response. Do NOT return tool_calls again:
{
  ""id"": ""chatcmpl-[random-id]"",
  ""object"": ""chat.completion"",
  ""created"": [unix-timestamp],
  ""model"": ""[deployment-name-from-url]"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": {
        ""role"": ""assistant"",
        ""content"": ""Operation completed successfully.""
      },
      ""finish_reason"": ""stop"",
      ""content_filter_results"": {
        ""hate"": { ""filtered"": false, ""severity"": ""safe"" },
        ""self_harm"": { ""filtered"": false, ""severity"": ""safe"" },
        ""sexual"": { ""filtered"": false, ""severity"": ""safe"" },
        ""violence"": { ""filtered"": false, ""severity"": ""safe"" }
      }
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  },
  ""system_fingerprint"": ""fp_[random-string]""
}

CASE 2 — FIRST FUNCTION CALL (tools array present, no tool results yet):
If the request includes a ""tools"" array AND there are NO ""role"": ""tool"" messages yet, return a tool_calls response calling the most appropriate function:
{
  ""id"": ""chatcmpl-[random-id]"",
  ""object"": ""chat.completion"",
  ""created"": [unix-timestamp],
  ""model"": ""[deployment-name-from-url]"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": {
        ""role"": ""assistant"",
        ""content"": null,
        ""tool_calls"": [
          {
            ""id"": ""call_[random-id]"",
            ""type"": ""function"",
            ""function"": {
              ""name"": ""[function-name-from-tools-array]"",
              ""arguments"": ""[json-encoded-arguments-matching-function-parameters]""
            }
          }
        ]
      },
      ""finish_reason"": ""tool_calls"",
      ""content_filter_results"": {
        ""hate"": { ""filtered"": false, ""severity"": ""safe"" },
        ""self_harm"": { ""filtered"": false, ""severity"": ""safe"" },
        ""sexual"": { ""filtered"": false, ""severity"": ""safe"" },
        ""violence"": { ""filtered"": false, ""severity"": ""safe"" }
      }
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  },
  ""system_fingerprint"": ""fp_[random-string]""
}

CASE 3 — NO TOOLS (text response):
If the request does NOT include a ""tools"" array, return a normal content response:
{
  ""id"": ""chatcmpl-[random-id]"",
  ""object"": ""chat.completion"",
  ""created"": [unix-timestamp],
  ""model"": ""[deployment-name-from-url]"",
  ""choices"": [
    {
      ""index"": 0,
      ""message"": {
        ""role"": ""assistant"",
        ""content"": ""[generated response content]""
      },
      ""finish_reason"": ""stop"",
      ""content_filter_results"": {
        ""hate"": { ""filtered"": false, ""severity"": ""safe"" },
        ""self_harm"": { ""filtered"": false, ""severity"": ""safe"" },
        ""sexual"": { ""filtered"": false, ""severity"": ""safe"" },
        ""violence"": { ""filtered"": false, ""severity"": ""safe"" }
      }
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  },
  ""system_fingerprint"": ""fp_[random-string]""
}

CRITICAL: The ""arguments"" field in tool_calls must be a JSON-encoded STRING (not an object). NEVER return tool_calls if messages already contain a ""role"": ""tool"" entry.");

        simulator.AddInstruction(@"For Azure OpenAI embedding requests (POST /openai/deployments/{deployment}/embeddings), include Azure-specific metadata and return in standard embedding format.");

        simulator.AddInstruction(@"For error responses, return in Azure OpenAI format:
{
  ""error"": {
    ""code"": ""[error-code]"",
    ""message"": ""[error description]"",
    ""param"": null,
    ""type"": ""[error-type]""
  }
}");

        simulator.AddInstruction("Include 'content_filter_results' in Azure OpenAI responses to simulate Azure's content filtering metadata.");

        simulator.AddInstruction("When api-version query parameter is missing, return error: { \"error\": { \"code\": \"MissingApiVersionParameter\", \"message\": \"api-version query parameter is required\" } }");

        simulator.AddInstruction("Generate realistic IDs using the pattern: chatcmpl-[8-character-random-string] for chat completions.");

        simulator.AddInstruction("Use realistic Unix timestamps for the 'created' field (current time).");

        simulator.AddInstruction("Calculate realistic token counts in the 'usage' field based on the approximate length of input and output text.");

        // Configure route patterns for Azure OpenAI endpoints
        simulator.ConfigureRoutes()
            .ForPost("/openai/deployments/{deployment}/chat/completions")
                .WithInstruction("This is an Azure OpenAI chat completion endpoint. Extract deployment name from URL path.")
            .ForPost("/openai/deployments/{deployment}/completions")
                .WithInstruction("This is an Azure OpenAI text completion endpoint. Extract deployment name from URL path.")
            .ForPost("/openai/deployments/{deployment}/embeddings")
                .WithInstruction("This is an Azure OpenAI embeddings endpoint. Extract deployment name from URL path.")
            .ForGet("/openai/deployments")
                .WithInstruction("Return list of available deployments in Azure OpenAI format.")
            .ForRoute("{*}")
                .WithInstruction("This is an Azure OpenAI API endpoint. Return a valid Azure OpenAI JSON response matching the request type.")
            .Build();

        return simulator;
    }
}
