using System.Net;
using System.Text.Json;
using Augustus.AI;
using FluentAssertions;

namespace Augustus.AI.Tests;

public class JevTests
{
    private static readonly AIGenerationContext Context = new(
        "POST",
        "/v1/charges",
        "curl -X POST -d 'amount=2000&currency=usd' \"http://localhost/v1/charges\"",
        new[] { "Return a Stripe charge.", "Use status succeeded." });

    private static readonly IReadOnlyList<AIModelTier> Tiers = new[]
    {
        new AIModelTier("fast", "A single resource echoed from the request."),
        new AIModelTier("default", "A few related objects."),
        new AIModelTier("strong", "Many interacting rules.")
    };

    [Fact]
    public async Task Router_SendsScoreQuestionWithTierDescriptionsAsLevels()
    {
        var stub = new StubHttpHandler(_ => Answers("{\"tier\":{\"type\":\"score\",\"probabilities\":{\"0\":0.9,\"1\":0.1,\"2\":0}}}"));
        var router = new JevModelRouter(Jev(stub));

        var tier = await router.SelectTierAsync(Context, Tiers, CancellationToken.None);

        tier.Should().Be(0);
        var request = stub.Requests.Single();
        request.Uri.ToString().Should().Be("https://api.typesafe.ai/v1/systemone");
        request.Json.GetProperty("model").GetString().Should().Be("jev-1.13.0");
        var question = request.Json.GetProperty("questions").GetProperty("tier");
        question.GetProperty("type").GetString().Should().Be("score");
        question.GetProperty("criteria").EnumerateArray().Select(c => c.GetString())
            .Should().Equal(Tiers.Select(t => t.Description));
        request.Json.GetProperty("state").GetProperty("request").GetString().Should().Contain("amount=2000");
    }

    [Theory]
    [InlineData(0.6, 0.25, 0.15, 1)]
    [InlineData(0.8, 0.1, 0.1, 0)]
    [InlineData(0.3, 0.3, 0.4, 2)]
    public void Router_PicksLowestTierWhoseCumulativeProbabilityReachesConfidence(double p0, double p1, double p2, int expected)
    {
        using var answers = JsonDocument.Parse(
            $"{{\"tier\":{{\"probabilities\":{{\"0\":{p0},\"1\":{p1},\"2\":{p2}}}}}}}");

        JevModelRouter.SelectLowestSufficientTier(answers.RootElement, 3, 0.8).Should().Be(expected);
    }

    [Fact]
    public async Task Router_FallsBackToFirstTierWhenJevFails()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.Json("{}", HttpStatusCode.Unauthorized));

        var tier = await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers, CancellationToken.None);

        tier.Should().Be(0);
    }

    [Fact]
    public void Validator_AsksOneNoulPerInstructionPlusObjectAndPlaceholderChecks()
    {
        var questions = JevResponseValidator.BuildQuestions(Context.Instructions);

        questions.Select(q => q.Key).Should().BeEquivalentTo("wrong_object", "placeholder", "instruction_0", "instruction_1");
        questions["instruction_1"]!["instructions"]!["instruction"]!.GetValue<string>().Should().Be("Use status succeeded.");
    }

    [Fact]
    public async Task Validator_RejectsWhenAnyCheckIsAboveThreshold()
    {
        var stub = new StubHttpHandler(_ => Answers(
            "{\"wrong_object\":{\"noul\":0.1},\"placeholder\":{\"noul\":0.2},\"instruction_0\":{\"noul\":0.05},\"instruction_1\":{\"noul\":0.91}}"));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().Contain("Use status succeeded.").And.Contain("0.91");
        stub.Requests.Single().Json.GetProperty("state").GetProperty("response").GetProperty("amount").GetInt32().Should().Be(2000);
    }

    [Fact]
    public async Task Validator_PassesWhenEveryCheckIsAtOrBelowThreshold()
    {
        var stub = new StubHttpHandler(_ => Answers(
            "{\"wrong_object\":{\"noul\":0.1},\"placeholder\":{\"noul\":0.7},\"instruction_0\":{\"noul\":0.05},\"instruction_1\":{\"noul\":0.3}}"));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validator_RetriesRateLimitThenSucceeds()
    {
        var calls = 0;
        var stub = new StubHttpHandler(_ => ++calls == 1
            ? StubHttpHandler.Json("{}", HttpStatusCode.TooManyRequests)
            : Answers("{\"wrong_object\":{\"noul\":0.1}}"));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
        stub.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Validator_SkipsTheCheckWhenJevFails()
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.Json("{}", HttpStatusCode.Unauthorized));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("{\"model\":\"jev-1.13.0\"}")]
    [InlineData("{\"answers\":[]}")]
    [InlineData("{\"answers\":{\"tier\":{\"probabilities\":[0.9]}}}")]
    public async Task Router_FallsBackToFirstTierOnMalformedAnswers(string body)
    {
        var stub = new StubHttpHandler(_ => StubHttpHandler.Json(body));

        var tier = await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers, CancellationToken.None);

        tier.Should().Be(0);
    }

    [Fact]
    public async Task Router_PropagatesCallerCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var stub = new StubHttpHandler(_ => Answers("{}"));

        var act = async () => await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void Constructors_RequireAnApiKey()
    {
        ((Action)(() => new JevModelRouter(new JevOptions()))).Should().Throw<ArgumentException>();
        ((Action)(() => new JevResponseValidator(new JevOptions()))).Should().Throw<ArgumentException>();
    }

    private static JevOptions Jev(StubHttpHandler stub) => new() { ApiKey = "ts-test", HttpHandlerOverride = stub };

    private static HttpResponseMessage Answers(string answers)
        => StubHttpHandler.Json($"{{\"model\":\"jev-1.13.0\",\"answers\":{answers}}}");

    private static JsonElement Response()
        => JsonDocument.Parse("{\"object\":\"charge\",\"amount\":2000,\"status\":\"pending\"}").RootElement;
}
