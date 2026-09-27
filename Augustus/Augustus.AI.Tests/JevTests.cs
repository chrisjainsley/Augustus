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
    public async Task Router_SendsOneRiskQuestionOverTheRequest()
    {
        var stub = new StubHttpHandler(_ => Answers("{\"risky\":{\"type\":\"noul\",\"noul\":0.2}}"));

        await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers, CancellationToken.None);

        var request = stub.Requests.Single();
        request.Uri.ToString().Should().Be("https://api.typesafe.ai/v1/systemone");
        request.Json.GetProperty("model").GetString().Should().Be("jev-1.13.0");
        request.Json.GetProperty("questions").GetProperty("risky").GetProperty("type").GetString().Should().Be("noul");
        request.Json.GetProperty("state").GetProperty("request").GetString().Should().Contain("amount=2000");
    }

    [Theory]
    [InlineData(0.21, 0)]
    [InlineData(0.70, 0)]
    [InlineData(0.75, 1)]
    public async Task Router_StartsAtSecondTierOnlyWhenRiskIsAboveThreshold(double risk, int expected)
    {
        var stub = new StubHttpHandler(_ => Answers($"{{\"risky\":{{\"noul\":{risk.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}"));

        var tier = await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers, CancellationToken.None);

        tier.Should().Be(expected);
    }

    [Fact]
    public async Task Router_SkipsJevWithASingleTier()
    {
        var stub = new StubHttpHandler(_ => Answers("{\"risky\":{\"noul\":0.99}}"));

        var tier = await new JevModelRouter(Jev(stub)).SelectTierAsync(Context, Tiers.Take(1).ToList(), CancellationToken.None);

        tier.Should().Be(0);
        stub.Requests.Should().BeEmpty();
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
        stub.Requests.First().Json.GetProperty("state").GetProperty("response").GetProperty("amount").GetInt32().Should().Be(2000);
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
    [InlineData("{\"answers\":{\"risky\":{\"noul\":\"high\"}}}")]
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
    public async Task Validator_NamesTheFieldThatBreaksTheInstruction()
    {
        var stub = new StubHttpHandler(r => r.Body.Contains("\"field\"")
            ? Answers("{\"field\":{\"type\":\"choice\",\"choice\":\"status\",\"confidence\":0.9}}")
            : Answers("{\"wrong_object\":{\"noul\":0.1},\"placeholder\":{\"noul\":0.1},\"instruction_0\":{\"noul\":0.1},\"instruction_1\":{\"noul\":0.93}}"));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        stub.Requests.Should().HaveCount(2);
        result.Reason.Should().Contain("field `status` is \"pending\"").And.Contain("Use status succeeded.");
        var evidence = stub.Requests[1].Json.GetProperty("questions").GetProperty("field");
        evidence.GetProperty("type").GetString().Should().Be("choice");
        evidence.GetProperty("criteria").EnumerateObject().Select(c => c.Name)
            .Should().Contain(new[] { "object", "amount", "status", "(no single field)" });
    }

    [Fact]
    public async Task Validator_OmitsEvidenceWhenJevIsUnsure()
    {
        var stub = new StubHttpHandler(r => r.Body.Contains("\"field\"")
            ? Answers("{\"field\":{\"choice\":\"status\",\"confidence\":0.3}}")
            : Answers("{\"instruction_1\":{\"noul\":0.93}}"));

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, Response(), CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Reason.Should().NotContain("field `");
    }

    [Fact]
    public async Task Validator_DescribesTheShapeForAWrongObject()
    {
        var stub = new StubHttpHandler(_ => Answers("{\"wrong_object\":{\"noul\":0.83}}"));
        var error = JsonDocument.Parse("{\"error\":{\"message\":\"Received unknown parameter: items\"}}").RootElement;

        var result = await new JevResponseValidator(Jev(stub)).ValidateAsync(Context, error, CancellationToken.None);

        result.Reason.Should().Contain("top-level fields are error");
        stub.Requests.Should().ContainSingle();
    }

    [Fact]
    public void CandidateFields_ListsNestedPathsBreadthFirstAndSummarisesLists()
    {
        using var doc = JsonDocument.Parse(
            "{\"id\":\"ch_1\",\"outcome\":{\"risk\":{\"level\":\"normal\",\"deep\":{\"x\":1}}},\"refunds\":[1,2]}");

        var candidates = JevResponseValidator.CandidateFields(doc.RootElement);

        candidates.Select(c => c.Path).Should().Equal("id", "refunds", "outcome.risk.level", "outcome.risk.deep");
        candidates.Single(c => c.Path == "refunds").Preview.Should().Be("a list of 2");
        candidates.Single(c => c.Path == "outcome.risk.deep").Preview.Should().Be("an object");
    }

    [Fact]
    public void FindViolation_ReportsTheMostProbableCheck()
    {
        using var answers = JsonDocument.Parse("{\"placeholder\":{\"noul\":0.75},\"instruction_1\":{\"noul\":0.95}}");

        var violation = JevResponseValidator.FindViolation(answers.RootElement, Context.Instructions, 0.7);

        violation!.Id.Should().Be("instruction_1");
    }

    [Fact]
    public void CandidateFields_ListsEmptyObjectsAndSkipsCollidingPaths()
    {
        using var doc = JsonDocument.Parse("{\"metadata\":{},\"a.b\":1,\"a\":{\"b\":2},\"(no single field)\":3}");

        var candidates = JevResponseValidator.CandidateFields(doc.RootElement);

        candidates.Select(c => c.Path).Should().Equal("metadata", "a.b");
        candidates[0].Preview.Should().Be("an empty object");
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
