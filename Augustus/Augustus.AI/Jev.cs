using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Augustus.AI;

/// <summary>
/// Settings for the TypeSafe Jev model used by <see cref="JevModelRouter"/> and <see cref="JevResponseValidator"/>.
/// </summary>
public sealed class JevOptions
{
    private string _apiKey = string.Empty;
    private string _model = "jev-1.13.0";
    private double _routerConfidence = 0.8;
    private double _rejectAbove = 0.7;

    /// <summary>Gets or sets the TypeSafe API key.</summary>
    public string ApiKey
    {
        get => _apiKey;
        set => _apiKey = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the Jev model. Pinned to a version by default so tuned thresholds do not drift when an alias moves.
    /// </summary>
    public string Model
    {
        get => _model;
        set => _model = string.IsNullOrWhiteSpace(value) ? "jev-1.13.0" : value.Trim();
    }

    /// <summary>
    /// Gets or sets how sure the router must be that a tier is enough before choosing it.
    /// The router picks the lowest tier whose cumulative probability reaches this value.
    /// </summary>
    public double RouterConfidence
    {
        get => _routerConfidence;
        set
        {
            if (value is <= 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(RouterConfidence), "RouterConfidence must be greater than 0 and at most 1");
            _routerConfidence = value;
        }
    }

    /// <summary>
    /// Gets or sets the probability above which a validator check counts as a violation.
    /// </summary>
    public double RejectAbove
    {
        get => _rejectAbove;
        set
        {
            if (value is <= 0 or >= 1)
                throw new ArgumentOutOfRangeException(nameof(RejectAbove), "RejectAbove must be between 0 and 1");
            _rejectAbove = value;
        }
    }

    /// <summary>Test hook: replaces the HTTP transport for TypeSafe calls.</summary>
    internal HttpMessageHandler? HttpHandlerOverride { get; set; }
}

/// <summary>
/// Picks the starting model tier by asking Jev to score the request against each tier's description.
/// Falls back to the first tier when Jev cannot be reached.
/// </summary>
public sealed class JevModelRouter : IAIModelRouter
{
    private const string QuestionId = "tier";
    private readonly JevOptions options;
    private readonly TypeSafeClient client;

    /// <summary>Creates a router that calls Jev with the given options.</summary>
    public JevModelRouter(JevOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("A TypeSafe API key is required.", nameof(options));
        client = new TypeSafeClient(options);
    }

    /// <inheritdoc />
    public async ValueTask<int> SelectTierAsync(
        AIGenerationContext context,
        IReadOnlyList<AIModelTier> tiers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(tiers);
        if (tiers.Count < 2)
            return 0;

        var question = new JsonObject
        {
            ["type"] = "score",
            ["instructions"] =
                "Which level describes the response `request` needs, given `instructions`? " +
                "Pick the simplest level that is enough to produce a correct response.",
            ["criteria"] = new JsonArray(tiers.Select(t => (JsonNode)JsonValue.Create(t.Description)!).ToArray())
        };

        JsonElement answers;
        try
        {
            answers = await client
                .AskAsync(RequestState(context), new JsonObject { [QuestionId] = question }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            Console.WriteLine($"[Jev Router] Falling back to the first tier: {ex.Message}");
            return 0;
        }

        return SelectLowestSufficientTier(answers, tiers.Count, options.RouterConfidence);
    }

    /// <summary>The lowest level whose cumulative probability reaches <paramref name="confidence"/>.</summary>
    internal static int SelectLowestSufficientTier(JsonElement answers, int tierCount, double confidence)
    {
        if (!answers.TryGetProperty(QuestionId, out var answer)
            || !answer.TryGetProperty("probabilities", out var probabilities))
            return 0;

        var cumulative = 0.0;
        for (var level = 0; level < tierCount; level++)
        {
            if (probabilities.TryGetProperty(level.ToString(CultureInfo.InvariantCulture), out var p)
                && p.TryGetDouble(out var value))
                cumulative += value;
            if (cumulative >= confidence)
                return level;
        }

        return tierCount - 1;
    }

    internal static JsonObject RequestState(AIGenerationContext context) => new()
    {
        ["request"] = context.SanitizedCurlRequest,
        ["instructions"] = new JsonArray(context.Instructions.Select(i => (JsonNode)JsonValue.Create(i)!).ToArray())
    };
}

/// <summary>
/// Rejects a generated response when Jev judges that it violates an instruction, is the wrong kind of object,
/// or contains placeholder values. Keep numeric and date checks in code: Jev is weak at them.
/// </summary>
public sealed class JevResponseValidator : IAIResponseValidator
{
    private const string WrongObjectId = "wrong_object";
    private const string PlaceholderId = "placeholder";
    private const string InstructionIdPrefix = "instruction_";

    private readonly JevOptions options;
    private readonly TypeSafeClient client;

    /// <summary>Creates a validator that calls Jev with the given options.</summary>
    public JevResponseValidator(JevOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("A TypeSafe API key is required.", nameof(options));
        client = new TypeSafeClient(options);
    }

    /// <inheritdoc />
    public async ValueTask<AIResponseValidationResult> ValidateAsync(
        AIGenerationContext context,
        JsonElement response,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var questions = BuildQuestions(context.Instructions);
        var state = JevModelRouter.RequestState(context);
        state["response"] = JsonNode.Parse(response.GetRawText());

        JsonElement answers;
        try
        {
            answers = await client.AskAsync(state, questions, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return AIResponseValidationResult.Invalid($"Jev could not check the response: {ex.Message}");
        }

        return Evaluate(answers, context.Instructions, options.RejectAbove);
    }

    internal static JsonObject BuildQuestions(IReadOnlyList<string> instructions)
    {
        var questions = new JsonObject
        {
            [WrongObjectId] = Noul(
                "Is `response` the wrong kind of object for `request`? For example a list where one resource was " +
                "requested, a different resource type, or an error when `instructions` describe success.",
                "`response` is the wrong kind of object for `request`",
                "`response` is the kind of object `request` should return"),
            [PlaceholderId] = Noul(
                "Does `response` contain placeholder values such as \"string\", \"example\", \"TODO\", \"lorem ipsum\" " +
                "or \"[value]\" instead of realistic data?",
                "at least one value is a placeholder",
                "every value looks like realistic data")
        };

        for (var i = 0; i < instructions.Count; i++)
        {
            questions[InstructionIdPrefix + i.ToString(CultureInfo.InvariantCulture)] = new JsonObject
            {
                ["type"] = "noul",
                ["instructions"] = new JsonObject
                {
                    ["instruction"] = instructions[i],
                    ["question"] = "Does `response` violate or ignore `instruction` for this `request`?"
                },
                ["criteria"] = new JsonObject
                {
                    ["true"] = "`response` violates or ignores the instruction",
                    ["false"] = "`response` follows the instruction, or the instruction does not apply to this request"
                }
            };
        }

        return questions;
    }

    internal static AIResponseValidationResult Evaluate(JsonElement answers, IReadOnlyList<string> instructions, double rejectAbove)
    {
        foreach (var answer in answers.EnumerateObject())
        {
            if (!answer.Value.TryGetProperty("noul", out var noul) || !noul.TryGetDouble(out var probability))
                continue;
            if (probability <= rejectAbove)
                continue;

            var reason = answer.Name switch
            {
                WrongObjectId => "the response is the wrong kind of object for the request",
                PlaceholderId => "the response contains placeholder values",
                _ when answer.Name.StartsWith(InstructionIdPrefix, StringComparison.Ordinal)
                    && int.TryParse(answer.Name.AsSpan(InstructionIdPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                    && index < instructions.Count
                    => $"the response violates the instruction \"{instructions[index]}\"",
                _ => $"check '{answer.Name}' failed"
            };
            return AIResponseValidationResult.Invalid(
                $"{reason} (p={probability.ToString("0.00", CultureInfo.InvariantCulture)})");
        }

        return AIResponseValidationResult.Valid;
    }

    private static JsonObject Noul(string question, string whenTrue, string whenFalse) => new()
    {
        ["type"] = "noul",
        ["instructions"] = question,
        ["criteria"] = new JsonObject { ["true"] = whenTrue, ["false"] = whenFalse }
    };
}

/// <summary>
/// Minimal client for the TypeSafe System One API, with backoff on 429, 529 and 5xx responses.
/// </summary>
internal sealed class TypeSafeClient
{
    internal const string Endpoint = "https://api.typesafe.ai/v1/systemone";
    private const int MaxAttempts = 3;
    private const int InitialDelayMs = 500;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly Lazy<HttpClient> SharedHttpClient = new(() => new HttpClient { Timeout = Timeout });

    private readonly JevOptions options;
    private readonly HttpClient httpClient;

    public TypeSafeClient(JevOptions options)
    {
        this.options = options;
        httpClient = options.HttpHandlerOverride is null
            ? SharedHttpClient.Value
            : new HttpClient(options.HttpHandlerOverride, disposeHandler: false) { Timeout = Timeout };
    }

    /// <summary>Asks <paramref name="questions"/> over <paramref name="state"/> and returns the <c>answers</c> object.</summary>
    public async Task<JsonElement> AskAsync(JsonObject state, JsonObject questions, CancellationToken cancellationToken)
    {
        var payload = new JsonObject
        {
            ["model"] = options.Model,
            ["state"] = state,
            ["questions"] = questions
        }.ToJsonString();

        var delayMs = InitialDelayMs;
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var document = JsonDocument.Parse(body);
                return document.RootElement.GetProperty("answers").Clone();
            }

            if (attempt >= MaxAttempts || !IsRetryable(response.StatusCode))
                throw new HttpRequestException($"TypeSafe returned HTTP {(int)response.StatusCode}.");

            var retryAfter = response.Headers.RetryAfter?.Delta;
            var wait = retryAfter is { } delta && delta > TimeSpan.Zero ? delta : TimeSpan.FromMilliseconds(delayMs);
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            delayMs *= 2;
        }
    }

    private static bool IsRetryable(HttpStatusCode status)
        => status == HttpStatusCode.TooManyRequests || (int)status == 529 || (int)status >= 500;
}
