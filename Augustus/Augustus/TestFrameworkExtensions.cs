namespace Augustus.Extensions;

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
    /// Creates a new API simulator with optional configuration.
    /// </summary>
    /// <param name="testClass">The test class instance (typically <c>this</c>).</param>
    /// <param name="configure">Optional action to configure simulator options.</param>
    /// <returns>A new <see cref="APISimulator"/> instance.</returns>
    /// <example>
    /// <code>
    /// var simulator = this.CreateAPISimulator(opt =>
    /// {
    ///     opt.OpenAIApiKey = "your-key";
    ///     opt.Port = 9001;
    /// });
    /// </code>
    /// </example>
    public static APISimulator CreateAPISimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        var options = new APISimulatorOptions();
        configure?.Invoke(options);

        return new APISimulator(options);
    }

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
    public static APISimulator CreateAPISimulator(this object testClass, string apiName, Action<APISimulatorOptions>? configure = null)
    {
        var options = new APISimulatorOptions();
        configure?.Invoke(options);

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
    public static APISimulator CreateStripeSimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        return testClass.CreateAPISimulator("Stripe", configure);
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
    public static APISimulator CreatePayPalSimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        return testClass.CreateAPISimulator("PayPal", configure);
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
    public static APISimulator CreateTwilioSimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        return testClass.CreateAPISimulator("Twilio", configure);
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
    public static APISimulator CreateSlackSimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        return testClass.CreateAPISimulator("Slack", configure);
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
    public static APISimulator CreateOpenAISimulator(this object testClass, Action<APISimulatorOptions>? configure = null)
    {
        var simulator = testClass.CreateAPISimulator("OpenAI", configure);

        // Add default instructions for OpenAI API format
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
      ""finish_reason"": ""stop""
    }
  ],
  ""usage"": {
    ""prompt_tokens"": [realistic-number],
    ""completion_tokens"": [realistic-number],
    ""total_tokens"": [sum-of-tokens]
  }
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
        await simulator.StartAsync(cancellationToken);
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