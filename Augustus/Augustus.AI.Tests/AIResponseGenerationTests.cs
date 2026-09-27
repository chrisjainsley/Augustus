using System.Net;
using System.Text;
using System.Text.Json;
using Augustus;
using Augustus.AI;
using Augustus.Extensions;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class AIResponseGenerationTests : IDisposable
{
    private const string ValidBody = "{\"object\":\"charge\",\"amount\":2000}";
    private readonly string cacheDir = Path.Combine(Path.GetTempPath(), $"aug_gen_{Guid.NewGuid():N}");

    public AIResponseGenerationTests()
    {
        Directory.CreateDirectory(cacheDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(cacheDir, true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public async Task GivenTierWithCustomEndpoint_ThenChatCallGoesToThatEndpoint()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = Options(stub);
        options.ModelTiers.Add(new AIModelTier("gpt-oss-20b", "fast") { Endpoint = "https://api.groq.test/openai/v1" });

        var (status, _) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.OK);
        stub.Requests.Should().ContainSingle();
        stub.Requests[0].Uri.Host.Should().Be("api.groq.test");
        stub.Requests[0].Model.Should().Be("gpt-oss-20b");
    }

    [Fact]
    public async Task GivenOpenAIEndpointWithoutAzure_ThenChatCallGoesToThatEndpoint()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = Options(stub);
        options.OpenAIEndpoint = "https://api.cerebras.test/v1";

        await PostChargeAsync(options);

        stub.Requests.Single().Uri.Host.Should().Be("api.cerebras.test");
    }

    [Fact]
    public async Task GivenDefaultModel_ThenReasoningEffortNoneIsSentWithoutTemperature()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));

        await PostChargeAsync(Options(stub));

        var request = stub.Requests.Single().Json;
        request.GetProperty("model").GetString().Should().Be("gpt-6-luna");
        request.GetProperty("reasoning_effort").GetString().Should().Be("none");
        request.TryGetProperty("temperature", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GivenNonReasoningModel_ThenNoReasoningEffortIsSent()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = Options(stub);
        options.OpenAIModel = "gpt-4o-mini";
        options.MaxOutputTokens = 800;

        await PostChargeAsync(options);

        var request = stub.Requests.Single().Json;
        request.TryGetProperty("reasoning_effort", out _).Should().BeFalse();
        request.GetProperty("temperature").GetDouble().Should().Be(0);
        request.GetProperty("max_completion_tokens").GetInt32().Should().Be(800);
    }

    [Fact]
    public async Task GivenRouter_ThenFirstChatCallUsesTheSelectedTier()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = ThreeTierOptions(stub);
        options.ModelRouter = new FixedRouter(2);

        await PostChargeAsync(options);

        stub.Requests.Single().Model.Should().Be("tier-2");
    }

    [Fact]
    public async Task GivenRejectedBody_ThenNextTierRegeneratesWithTheReasonAndResultIsCached()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = ThreeTierOptions(stub);
        options.ResponseValidators.Add(new RejectFirstValidator("amount does not match"));

        var (status, body) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("charge");
        stub.Requests.Select(r => r.Model).Should().Equal("tier-0", "tier-1");
        stub.Requests[1].Body.Should().Contain("amount does not match");
        var cached = CachedEntries().Should().ContainSingle().Subject;
        cached.GetProperty("Model").GetString().Should().Be("tier-1");
    }

    [Fact]
    public async Task GivenEveryBodyRejected_ThenFourCallsAre502AndNothingIsCached()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = ThreeTierOptions(stub);
        options.ResponseValidators.Add(new RejectAllValidator());

        var (status, body) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.BadGateway);
        stub.Requests.Should().HaveCount(4);
        stub.Requests.Select(r => r.Model).Should().Equal("tier-0", "tier-1", "tier-2", "tier-2");
        using var error = JsonDocument.Parse(body);
        error.RootElement.GetProperty("error").GetString().Should().Contain(nameof(RejectAllValidator));
        error.RootElement.GetProperty("attempts").GetInt32().Should().Be(4);
        CachedEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenThrowingRouter_ThenGenerationStartsAtTheFirstTier()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = ThreeTierOptions(stub);
        options.ModelRouter = new ThrowingRouter();

        var (status, _) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.OK);
        stub.Requests.Single().Model.Should().Be("tier-0");
    }

    [Fact]
    public async Task GivenThrowingValidator_ThenRequestFailsWithoutRegenerating()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = ThreeTierOptions(stub);
        options.ResponseValidators.Add(new ThrowingValidator());

        var (status, body) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.BadGateway);
        body.Should().Contain(nameof(ThrowingValidator)).And.Contain("InvalidOperationException");
        stub.Requests.Should().ContainSingle();
        CachedEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenBodyThatIsNotJson_ThenItIsRejectedAndNotCached()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion("not json"));
        var options = Options(stub);
        options.OpenAIModel = "gpt-4o-mini";
        options.MaxValidationRetries = 0;

        var (status, body) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.BadGateway);
        body.Should().Contain("not valid JSON");
        CachedEntries().Should().BeEmpty();
    }

    [Fact]
    public async Task GivenCachedResponse_ThenRouterAndValidatorsAreSkipped()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        await PostChargeAsync(Options(stub));

        var router = new FixedRouter(0);
        var options = Options(stub);
        options.ModelRouter = router;
        options.ResponseValidators.Add(new RejectAllValidator());
        var (status, _) = await PostChargeAsync(options);

        status.Should().Be(HttpStatusCode.OK);
        stub.Requests.Should().ContainSingle();
        router.Calls.Should().Be(0);
    }

    [Fact]
    public async Task GivenRouteLevelAI_ThenValidationFailureAlsoReturns502()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = Options(stub);
        options.MaxValidationRetries = 0;
        options.ResponseValidators.Add(new RejectAllValidator());

        var simulator = this.CreateAPISimulator("RouteGen", o =>
        {
            o.Port = 0;
            o.CacheFolderPath = cacheDir;
        });
        simulator.ForPost("/v1/charges").UseAI(options, "Return a Stripe charge").Add();

        await using (simulator)
        {
            await simulator.StartAsync();
            using var client = simulator.CreateClient();
            using var response = await client.PostAsync("/v1/charges", Form());
            response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
            (await response.Content.ReadAsStringAsync()).Should().Contain("\"attempts\":1");
        }
    }

    [Fact]
    public void GivenNoTiersAndEffortNone_ThenLadderClimbsEffortOnTheSameModel()
    {
        var tiers = new AIOptions().ResolveModelTiers();

        tiers.Select(t => t.ReasoningEffort).Should().Equal("none", "low", "medium", "high");
        tiers.Should().OnlyContain(t => t.Model == "gpt-6-luna");
    }

    [Fact]
    public void GivenExplicitNonReasoningModel_ThenLadderIsThatModelAlone()
    {
        var tiers = new AIOptions { OpenAIModel = "gpt-4o-mini" }.ResolveModelTiers();

        tiers.Should().ContainSingle().Which.ReasoningEffort.Should().BeNull();
    }

    [Theory]
    [InlineData("low", new[] { "low", "medium", "high" })]
    [InlineData("minimal", new[] { "minimal", "low", "medium", "high" })]
    [InlineData("high", new[] { "high" })]
    public void GivenExplicitEffort_ThenLadderStartsThere(string effort, string[] expected)
    {
        var tiers = new AIOptions { OpenAIModel = "gpt-oss-120b", ReasoningEffort = effort }.ResolveModelTiers();

        tiers.Select(t => t.ReasoningEffort).Should().Equal(expected);
    }

    [Fact]
    public void GivenUnknownEffort_ThenSetterThrows()
    {
        var act = () => new AIOptions { ReasoningEffort = "extreme" };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GivenOutOfRangeLimits_ThenSettersThrow()
    {
        ((Action)(() => new AIOptions { MaxOutputTokens = 0 })).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => new AIOptions { MaxValidationRetries = 6 })).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GivenEveryTierHasItsOwnKey_ThenGlobalKeyIsNotRequired()
    {
        var options = new AIOptions();
        options.ModelTiers.Add(new AIModelTier("gpt-oss-20b", "fast") { Endpoint = "https://api.groq.test/openai/v1", ApiKey = "gsk" });

        ((Action)options.Validate).Should().NotThrow();

        options.ModelTiers.Add(new AIModelTier("gpt-6-luna", "strong"));
        ((Action)options.Validate).Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>();
    }

    [Fact]
    public async Task GivenTiersSharingAModel_ThenConcurrentIdenticalRequestsDoNotShareAcrossEffortLevels()
    {
        // The delay keeps both calls in flight together, so a shared dedupe key would collapse them into one.
        var stub = new StubHttpHandler(_ =>
        {
            Thread.Sleep(200);
            return StubHttpHandler.ChatCompletion(ValidBody);
        });
        var low = Options(stub);
        low.ModelTiers.Add(new AIModelTier("same-model", "d") { ReasoningEffort = "low" });
        var high = Options(stub);
        high.ModelTiers.Add(new AIModelTier("same-model", "d") { ReasoningEffort = "high" });
        var context = new AIGenerationContext("POST", "/v1/charges", "curl -X POST", new[] { "Return a charge." });

        await Task.WhenAll(
            new AIResponseGenerator(low).GenerateAsync("same-hash", context, CancellationToken.None),
            new AIResponseGenerator(high).GenerateAsync("same-hash", context, CancellationToken.None));

        stub.Requests.Select(r => r.Json.GetProperty("reasoning_effort").GetString())
            .Should().BeEquivalentTo(new[] { "low", "high" });
    }

    [Fact]
    public void GivenAzureWithEveryTierOnItsOwnEndpoint_ThenAzureSettingsAreNotRequired()
    {
        var options = new AIOptions { OpenAIApiKey = "k", UseAzureOpenAI = true };
        options.ModelTiers.Add(new AIModelTier("gpt-oss-20b", "fast") { Endpoint = "https://api.groq.test/openai/v1" });

        ((Action)options.Validate).Should().NotThrow();

        options.ModelTiers.Add(new AIModelTier("prod-deployment", "strong"));
        ((Action)options.Validate).Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>();
    }

    [Fact]
    public async Task GivenTierWithBlankApiKey_ThenTheGlobalKeyIsSent()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.ChatCompletion(ValidBody));
        var options = Options(stub);
        options.ModelTiers.Add(new AIModelTier("m", "d") { ApiKey = "  " });

        await PostChargeAsync(options);

        stub.Requests.Single().Authorization.Should().Be("Bearer test-key");
    }

    [Fact]
    public void GivenTierWithInvalidEndpoint_ThenValidateThrows()
    {
        var options = new AIOptions { OpenAIApiKey = "k" };
        options.ModelTiers.Add(new AIModelTier("m", "d") { Endpoint = "not a uri" });

        ((Action)options.Validate).Should().Throw<System.ComponentModel.DataAnnotations.ValidationException>();
    }

    private AIOptions Options(StubHttpHandler stub) => new()
    {
        OpenAIApiKey = "test-key",
        HttpHandlerOverride = stub
    };

    private AIOptions ThreeTierOptions(StubHttpHandler stub)
    {
        var options = Options(stub);
        for (var i = 0; i < 3; i++)
            options.ModelTiers.Add(new AIModelTier($"tier-{i}", $"level {i}"));
        return options;
    }

    private async Task<(HttpStatusCode Status, string Body)> PostChargeAsync(AIOptions aiOptions)
    {
        var simulator = this.CreateAPISimulator("Gen", o =>
        {
            o.Port = 0;
            o.CacheFolderPath = cacheDir;
        });
        simulator.UseAI(aiOptions);
        simulator.AddInstruction("Return a Stripe charge for POST /v1/charges.");

        await using (simulator)
        {
            await simulator.StartAsync();
            using var client = simulator.CreateClient();
            using var response = await client.PostAsync("/v1/charges", Form());
            var body = await response.Content.ReadAsStringAsync();
            await simulator.RoutingHandler.DrainPendingCacheWritesAsync(CancellationToken.None);
            return (response.StatusCode, body);
        }
    }

    private static FormUrlEncodedContent Form() => new(new Dictionary<string, string>
    {
        ["amount"] = "2000",
        ["currency"] = "usd"
    });

    private List<JsonElement> CachedEntries() => Directory
        .GetFiles(cacheDir, "*.json", SearchOption.AllDirectories)
        .Select(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement.Clone())
        .ToList();

    private sealed class FixedRouter : IAIModelRouter
    {
        private readonly int tier;

        public FixedRouter(int tier) => this.tier = tier;

        public int Calls { get; private set; }

        public ValueTask<int> SelectTierAsync(AIGenerationContext context, IReadOnlyList<AIModelTier> tiers, CancellationToken cancellationToken)
        {
            Calls++;
            return new(tier);
        }
    }

    private sealed class RejectFirstValidator : IAIResponseValidator
    {
        private readonly string reason;
        private int calls;

        public RejectFirstValidator(string reason) => this.reason = reason;

        public ValueTask<AIResponseValidationResult> ValidateAsync(AIGenerationContext context, JsonElement response, CancellationToken cancellationToken)
            => new(Interlocked.Increment(ref calls) == 1 ? AIResponseValidationResult.Invalid(reason) : AIResponseValidationResult.Valid);
    }

    private sealed class ThrowingRouter : IAIModelRouter
    {
        public ValueTask<int> SelectTierAsync(AIGenerationContext context, IReadOnlyList<AIModelTier> tiers, CancellationToken cancellationToken)
            => throw new InvalidOperationException("router broke");
    }

    private sealed class ThrowingValidator : IAIResponseValidator
    {
        public ValueTask<AIResponseValidationResult> ValidateAsync(AIGenerationContext context, JsonElement response, CancellationToken cancellationToken)
            => throw new InvalidOperationException("validator broke");
    }

    private sealed class RejectAllValidator : IAIResponseValidator
    {
        public ValueTask<AIResponseValidationResult> ValidateAsync(AIGenerationContext context, JsonElement response, CancellationToken cancellationToken)
            => new(AIResponseValidationResult.Invalid("always"));
    }
}
