namespace Augustus.Extensions;

using System.Runtime.CompilerServices;

/// <summary>
/// Extension methods for test frameworks to simplify <see cref="APISimulator"/> usage.
/// </summary>
/// <remarks>
/// These extensions provide a fluent API for creating and configuring API simulators
/// within test methods. Works with xUnit, NUnit, MSTest, and other testing frameworks.
/// </remarks>
public static class TestFrameworkExtensions
{
    /// <summary>
    /// Creates a new API simulator with the specified name and configuration.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="apiName">The name of the API to simulate (e.g., "Stripe", "PayPal").</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> instance.</returns>
    /// <example>
    /// <code>
    /// var simulator = this.CreateAPISimulator("Stripe", opt =>
    /// {
    ///     opt.OpenAIApiKey = "your-key";
    ///     opt.Port = 9001;
    /// });
    /// </code>
    /// </example>
    public static APISimulator CreateAPISimulator(this object testClass, string apiName, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var options = new APISimulatorOptions();
        configure?.Invoke(options);
        options.TestClassFilePath = callerFilePath;

        return new APISimulator(apiName, options);
    }

    /// <summary>
    /// Creates a new Stripe API simulator with pre-configured context.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for Stripe API simulation.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <c>CreateAPISimulator("Stripe", configure)</c>.
    /// </remarks>
    public static APISimulator CreateStripeSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Stripe", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new PayPal API simulator with pre-configured context.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for PayPal API simulation.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <c>CreateAPISimulator("PayPal", configure)</c>.
    /// </remarks>
    public static APISimulator CreatePayPalSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("PayPal", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new Twilio API simulator with pre-configured context.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for Twilio API simulation.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <c>CreateAPISimulator("Twilio", configure)</c>.
    /// </remarks>
    public static APISimulator CreateTwilioSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Twilio", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new Slack API simulator with pre-configured context.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for Slack API simulation.</returns>
    /// <remarks>
    /// This is a convenience method equivalent to calling <c>CreateAPISimulator("Slack", configure)</c>.
    /// </remarks>
    public static APISimulator CreateSlackSimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        return testClass.CreateAPISimulator("Slack", configure, callerFilePath);
    }

    /// <summary>
    /// Creates a new OpenAI API simulator with pre-configured context and default instructions.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for OpenAI API simulation with proper response formats.</returns>
    /// <remarks>
    /// <para>This simulator comes pre-configured with instructions to:</para>
    /// <list type="bullet">
    /// <item>Return responses in the official OpenAI API JSON format</item>
    /// <item>Support common endpoints like /v1/chat/completions, /v1/completions, /v1/models, /v1/embeddings</item>
    /// <item>Include proper response fields (id, object, created, model, choices, usage)</item>
    /// <item>Handle error responses with correct OpenAI error format</item>
    /// </list>
    /// <para>You can add additional custom instructions using <c>.WithInstruction()</c> or route-specific configurations.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var simulator = this.CreateOpenAISimulator(opt =>
    /// {
    ///     opt.OpenAIApiKey = "your-key";
    ///     opt.Port = 9015;
    /// });
    /// await simulator.StartAsync();
    /// </code>
    /// </example>
    public static APISimulator CreateOpenAISimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var simulator = testClass.CreateAPISimulator("OpenAI", configure, callerFilePath);

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
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> configured for Azure OpenAI API simulation with proper response formats.</returns>
    /// <remarks>
    /// <para>This simulator comes pre-configured with instructions to:</para>
    /// <list type="bullet">
    /// <item>Return responses in the official Azure OpenAI API JSON format</item>
    /// <item>Support Azure-specific endpoints like /openai/deployments/{deployment}/chat/completions</item>
    /// <item>Include proper response fields with Azure-specific metadata (content_filter_results)</item>
    /// <item>Handle api-version query parameter requirements</item>
    /// <item>Support api-key header authentication</item>
    /// </list>
    /// <para>You can add additional custom instructions using <c>.WithInstruction()</c> or route-specific configurations.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var simulator = this.CreateAzureOpenAISimulator(opt =>
    /// {
    ///     opt.OpenAIApiKey = "your-key";
    ///     opt.Port = 9020;
    /// });
    /// await simulator.StartAsync();
    /// </code>
    /// </example>
    public static APISimulator CreateAzureOpenAISimulator(this object testClass, Action<APISimulatorOptions>? configure = null, [CallerFilePath] string callerFilePath = "")
    {
        var simulator = testClass.CreateAPISimulator("Azure OpenAI", configure, callerFilePath);

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

    /// <summary>
    /// Starts the simulator asynchronously and returns it for method chaining.
    /// </summary>
    /// <param name="simulator">The simulator to start.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The same <see cref="APISimulator"/> instance after it has been started.</returns>
    /// <remarks>
    /// This extension enables fluent syntax for starting the simulator as part of a configuration chain.
    /// </remarks>
    /// <example>
    /// <code>
    /// var simulator = await this.CreateStripeSimulator(opt => opt.OpenAIApiKey = key)
    ///     .WithInstruction("Return realistic responses")
    ///     .StartSimulatorAsync();
    /// </code>
    /// </example>
    public static async Task<APISimulator> StartSimulatorAsync(this APISimulator simulator, CancellationToken cancellationToken = default)
    {
        await simulator.StartAsync(cancellationToken).ConfigureAwait(false);
        return simulator;
    }

    /// <summary>
    /// Adds a global instruction to the simulator and returns it for method chaining.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="instruction">The instruction to add.</param>
    /// <returns>The same <see cref="APISimulator"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// This extension enables fluent syntax for adding instructions during simulator configuration.
    /// </remarks>
    public static APISimulator WithInstruction(this APISimulator simulator, string instruction)
    {
        simulator.AddInstruction(instruction);
        return simulator;
    }

    /// <summary>
    /// Adds multiple global instructions to the simulator and returns it for method chaining.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="instructions">The instructions to add.</param>
    /// <returns>The same <see cref="APISimulator"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// This extension enables fluent syntax for adding multiple instructions at once.
    /// </remarks>
    public static APISimulator WithInstructions(this APISimulator simulator, params string[] instructions)
    {
        foreach (var instruction in instructions)
        {
            simulator.AddInstruction(instruction);
        }
        return simulator;
    }

    /// <summary>
    /// Creates a builder for configuring route-specific instructions.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <returns>An <see cref="InstructionBuilder"/> for fluent route configuration.</returns>
    /// <remarks>
    /// This is an extension method wrapper around <see cref="APISimulator.ConfigureRoutes"/>.
    /// </remarks>
    public static InstructionBuilder ConfigureRoutes(this APISimulator simulator)
    {
        return simulator.ConfigureRoutes();
    }

    /// <summary>
    /// Configures route-specific instructions for a given URL pattern and HTTP method.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers/{id}").</param>
    /// <param name="httpMethod">The HTTP method to match (e.g., "GET", "POST", or "*" for all methods). Default is "*".</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    /// <remarks>
    /// Route patterns support placeholders like {id} which match any value in that position.
    /// </remarks>
    public static InstructionBuilder ForRoute(this APISimulator simulator, string pattern, string httpMethod = "*")
    {
        return simulator.ConfigureRoutes().ForRoute(pattern, httpMethod);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP GET requests matching the pattern.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    public static InstructionBuilder ForGet(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForGet(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP POST requests matching the pattern.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers").</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    public static InstructionBuilder ForPost(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPost(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP PUT requests matching the pattern.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    public static InstructionBuilder ForPut(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPut(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP DELETE requests matching the pattern.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    public static InstructionBuilder ForDelete(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForDelete(pattern);
    }

    /// <summary>
    /// Configures route-specific instructions for HTTP PATCH requests matching the pattern.
    /// </summary>
    /// <param name="simulator">The simulator to configure.</param>
    /// <param name="pattern">The route pattern to match (e.g., "/api/customers/{id}").</param>
    /// <returns>An <see cref="InstructionBuilder"/> for adding instructions to this route.</returns>
    public static InstructionBuilder ForPatch(this APISimulator simulator, string pattern)
    {
        return simulator.ConfigureRoutes().ForPatch(pattern);
    }
}