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
    internal const string DefaultModel = "jev-1.13.0";
    internal const double DefaultRiskThreshold = 0.7;
    internal const double DefaultRejectAbove = 0.7;

    private string _apiKey = string.Empty;
    private string _model = DefaultModel;
    private double _riskThreshold = DefaultRiskThreshold;
    private double _rejectAbove = DefaultRejectAbove;

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
        set => _model = string.IsNullOrWhiteSpace(value) ? DefaultModel : value.Trim();
    }

    /// <summary>
    /// Gets or sets the probability that the first tier gets a request wrong above which the router starts at the
    /// second tier instead. Validation still escalates any detected failure, so the router only skips the first tier
    /// when a failure is likely. 1 never skips it.
    /// </summary>
    public double RiskThreshold
    {
        get => _riskThreshold;
        set
        {
            if (value is <= 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(RiskThreshold), "RiskThreshold must be greater than 0 and at most 1");
            _riskThreshold = value;
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
/// Starts generation at the first tier unless Jev judges that a fast answer is likely to be wrong, in which case it
/// starts at the second tier. Validation escalates any failure it detects, so the router only has to spot likely
/// failures up front. Falls back to the first tier when Jev cannot be reached.
/// </summary>
public sealed class JevModelRouter : IAIModelRouter
{
    private const string QuestionId = "risky";
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

        JsonElement answers;
        try
        {
            answers = await client
                .AskAsync(RequestState(context), new JsonObject { [QuestionId] = RiskQuestion() }, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"[Jev Router] Falling back to the first tier: {ex.Message}");
            return 0;
        }

        return JevAnswers.TryGetNoul(answers, QuestionId, out var risk) && risk > options.RiskThreshold ? 1 : 0;
    }

    internal static JsonObject RiskQuestion() => new()
    {
        ["type"] = "noul",
        ["instructions"] =
            "Would a fast model answering without deliberation likely return a wrong response to `request`: an error " +
            "instead of the success the caller intends, a wrong object type, or an instruction in `instructions` that " +
            "applies to this request left unmet? Unusual request formats, lists with exact counts and conditional rules " +
            "raise the risk; a routine create or retrieve of one resource does not.",
        ["criteria"] = new JsonObject
        {
            ["true"] = "a fast answer is likely to be wrong",
            ["false"] = "a fast answer is likely to be right"
        }
    };

    internal static JsonObject RequestState(AIGenerationContext context) => new()
    {
        ["request"] = context.SanitizedCurlRequest,
        ["instructions"] = new JsonArray(context.Instructions.Select(i => (JsonNode)JsonValue.Create(i)!).ToArray())
    };
}

/// <summary>
/// Rejects a generated response when Jev judges that it violates an instruction, is the wrong kind of object,
/// or contains placeholder values. Each rejection names the field that shows the problem, so the regenerated
/// response knows what to fix. Keep numeric and date checks in code: Jev is weak at them.
/// </summary>
public sealed class JevResponseValidator : IAIResponseValidator
{
    private const string WrongObjectId = "wrong_object";
    private const string PlaceholderId = "placeholder";
    private const string InstructionIdPrefix = "instruction_";
    private const string EvidenceId = "field";
    private const string NoSingleField = "(no single field)";
    private const double EvidenceConfidence = 0.5;
    private const int MaxEvidenceCandidates = 200;
    private const int MaxEvidenceDepth = 3;
    private const int MaxPreviewLength = 60;
    private const int MaxListedFields = 5;
    private static readonly TimeSpan EvidenceTimeout = TimeSpan.FromSeconds(5);

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

        var state = JevModelRouter.RequestState(context);
        state["response"] = JsonNode.Parse(response.GetRawText());

        JsonElement answers;
        try
        {
            answers = await client.AskAsync(state, BuildQuestions(context.Instructions), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // An outage is not a defect in the response; rejecting would regenerate for nothing.
            // Deterministic validators still run, so the response is not left unchecked.
            Console.WriteLine($"[Jev Validator] Skipping the Jev check: {ex.Message}");
            return AIResponseValidationResult.Valid;
        }

        var violation = FindViolation(answers, context.Instructions, options.RejectAbove);
        if (violation is null)
            return AIResponseValidationResult.Valid;

        var evidence = violation.Id == WrongObjectId
            ? DescribeShape(response)
            : violation.Id == PlaceholderId || violation.InstructionIndex is not null
                ? await FindEvidenceAsync(state, violation, context.Instructions, response, cancellationToken).ConfigureAwait(false)
                : null;

        var probability = violation.Probability.ToString("0.00", CultureInfo.InvariantCulture);
        return AIResponseValidationResult.Invalid(evidence is null
            ? $"{violation.Reason} (p={probability})"
            : $"{violation.Reason}: {evidence} (p={probability})");
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

    /// <summary>The most probable check above <paramref name="rejectAbove"/>, or null.</summary>
    internal static Violation? FindViolation(JsonElement answers, IReadOnlyList<string> instructions, double rejectAbove)
    {
        if (answers.ValueKind != JsonValueKind.Object)
            return null;

        var (name, probability) = answers.EnumerateObject()
            .Select(answer => (answer.Name, Found: JevAnswers.TryGetNoul(answers, answer.Name, out var p), Probability: p))
            .Where(a => a.Found && a.Probability > rejectAbove)
            .OrderByDescending(a => a.Probability)
            .Select(a => (a.Name, a.Probability))
            .FirstOrDefault();
        if (name is null)
            return null;

        if (name == WrongObjectId)
            return new Violation(WrongObjectId, "the response is the wrong kind of object for the request", null, probability);
        if (name == PlaceholderId)
            return new Violation(PlaceholderId, "the response contains placeholder values", null, probability);
        if (name.StartsWith(InstructionIdPrefix, StringComparison.Ordinal)
            && int.TryParse(name.AsSpan(InstructionIdPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var index)
            && index < instructions.Count)
            return new Violation(name, $"the response violates the instruction \"{instructions[index]}\"", index, probability);

        return new Violation(name, $"check '{name}' failed", null, probability);
    }

    /// <summary>
    /// Asks Jev which field shows the violation, choosing among fields listed in code so the answer is always a
    /// real path. Returns null when Jev cannot single one out or cannot be reached.
    /// </summary>
    private async Task<string?> FindEvidenceAsync(
        JsonObject state,
        Violation violation,
        IReadOnlyList<string> instructions,
        JsonElement response,
        CancellationToken cancellationToken)
    {
        var candidates = CandidateFields(response);
        if (candidates.Count == 0)
            return null;

        // Evidence only improves the message, so it gets one short attempt instead of the full retry budget.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(EvidenceTimeout);
        JsonElement answers;
        try
        {
            answers = await client
                .AskAsync(state, new JsonObject { [EvidenceId] = EvidenceQuestion(violation, instructions, candidates) }, timeout.Token, maxAttempts: 1)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        return SelectEvidence(answers, candidates);
    }

    internal static JsonObject EvidenceQuestion(Violation violation, IReadOnlyList<string> instructions, IReadOnlyList<FieldCandidate> candidates)
    {
        var criteria = new JsonObject();
        foreach (var candidate in candidates)
            criteria[candidate.Path] = $"`response.{candidate.Path}` = {candidate.Preview}";
        criteria[NoSingleField] = "no single field shows it: a required field is missing, or the problem is the response as a whole";

        JsonNode question = violation.InstructionIndex is { } index
            ? new JsonObject
            {
                ["instruction"] = instructions[index],
                ["question"] = "Which field of `response` breaks `instruction` for this `request`?"
            }
            : "Which field of `response` holds a placeholder value instead of realistic data?";

        return new JsonObject { ["type"] = "choice", ["instructions"] = question, ["criteria"] = criteria };
    }

    internal static string? SelectEvidence(JsonElement answers, IReadOnlyList<FieldCandidate> candidates)
    {
        if (answers.ValueKind != JsonValueKind.Object
            || !answers.TryGetProperty(EvidenceId, out var answer)
            || answer.ValueKind != JsonValueKind.Object
            || !answer.TryGetProperty("choice", out var choice)
            || choice.ValueKind != JsonValueKind.String)
            return null;
        if (answer.TryGetProperty("confidence", out var confidence)
            && confidence.ValueKind == JsonValueKind.Number
            && confidence.GetDouble() < EvidenceConfidence)
            return null;

        var path = choice.GetString();
        var candidate = candidates.FirstOrDefault(c => c.Path == path);
        return candidate is null ? null : $"field `{candidate.Path}` is {candidate.Preview}";
    }

    /// <summary>
    /// Field paths with a value preview, breadth first so top-level fields come first when the list is capped.
    /// Arrays are listed with their length and not descended into.
    /// </summary>
    internal static IReadOnlyList<FieldCandidate> CandidateFields(JsonElement response)
    {
        var candidates = new List<FieldCandidate>();
        if (response.ValueKind != JsonValueKind.Object)
            return candidates;

        // A key containing a dot can spell the same path as a nested field; the first one listed wins.
        var seen = new HashSet<string>(StringComparer.Ordinal) { NoSingleField };

        var queue = new Queue<(string Prefix, JsonElement Element, int Depth)>();
        queue.Enqueue((string.Empty, response, 1));
        while (queue.Count > 0 && candidates.Count < MaxEvidenceCandidates)
        {
            var (prefix, element, depth) = queue.Dequeue();
            foreach (var property in element.EnumerateObject())
            {
                if (candidates.Count >= MaxEvidenceCandidates)
                    break;
                var path = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
                if (!seen.Add(path))
                    continue;
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.Object when !property.Value.EnumerateObject().Any():
                        candidates.Add(new FieldCandidate(path, "an empty object"));
                        break;
                    case JsonValueKind.Object when depth < MaxEvidenceDepth:
                        queue.Enqueue((path, property.Value, depth + 1));
                        break;
                    case JsonValueKind.Object:
                        candidates.Add(new FieldCandidate(path, "an object"));
                        break;
                    case JsonValueKind.Array:
                        candidates.Add(new FieldCandidate(path, $"a list of {property.Value.GetArrayLength()}"));
                        break;
                    default:
                        candidates.Add(new FieldCandidate(path, Preview(property.Value.GetRawText())));
                        break;
                }
            }
        }

        return candidates;
    }

    /// <summary>For a wrong-object verdict, the shape says more than any one field.</summary>
    internal static string DescribeShape(JsonElement response)
    {
        if (response.ValueKind != JsonValueKind.Object)
            return $"the response is a JSON {response.ValueKind.ToString().ToLowerInvariant()}";
        if (response.TryGetProperty("object", out var kind) && kind.ValueKind == JsonValueKind.String)
            return $"field `object` is \"{kind.GetString()}\"";

        var fields = response.EnumerateObject().Select(p => p.Name).Take(MaxListedFields + 1).ToList();
        var listed = string.Join(", ", fields.Take(MaxListedFields));
        return fields.Count > MaxListedFields ? $"top-level fields are {listed}, ..." : $"top-level fields are {listed}";
    }

    private static string Preview(string raw)
        => raw.Length <= MaxPreviewLength ? raw : raw[..MaxPreviewLength] + "...";

    private static JsonObject Noul(string question, string whenTrue, string whenFalse) => new()
    {
        ["type"] = "noul",
        ["instructions"] = question,
        ["criteria"] = new JsonObject { ["true"] = whenTrue, ["false"] = whenFalse }
    };

    internal sealed record Violation(string Id, string Reason, int? InstructionIndex, double Probability);

    internal sealed record FieldCandidate(string Path, string Preview);
}

/// <summary>Reads typed answers from a TypeSafe response without trusting its shape.</summary>
internal static class JevAnswers
{
    public static bool TryGetNoul(JsonElement answers, string questionId, out double probability)
    {
        probability = 0;
        return answers.ValueKind == JsonValueKind.Object
            && answers.TryGetProperty(questionId, out var answer)
            && answer.ValueKind == JsonValueKind.Object
            && answer.TryGetProperty("noul", out var noul)
            && noul.ValueKind == JsonValueKind.Number
            && noul.TryGetDouble(out probability);
    }
}

/// <summary>
/// Minimal client for the TypeSafe System One API, with backoff on 429, 529 and 5xx responses.
/// </summary>
internal sealed class TypeSafeClient
{
    internal const string Endpoint = "https://api.typesafe.ai/v1/systemone";
    private const int MaxAttempts = 3;
    private const int InitialDelayMs = 500;
    private const int OverloadedStatusCode = 529;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(5);
    private static readonly Lazy<HttpClient> SharedHttpClient = new(() =>
        new HttpClient(new SocketsHttpHandler { PooledConnectionLifetime = PooledConnectionLifetime }) { Timeout = Timeout });

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
    public async Task<JsonElement> AskAsync(JsonObject state, JsonObject questions, CancellationToken cancellationToken, int maxAttempts = MaxAttempts)
    {
        var payload = new JsonObject
        {
            ["model"] = options.Model,
            // Cloned so callers can reuse a node across calls; a JsonNode can only have one parent.
            ["state"] = state.DeepClone(),
            ["questions"] = questions.DeepClone()
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
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("answers", out var answers)
                    || answers.ValueKind != JsonValueKind.Object)
                    throw new JsonException("TypeSafe response has no answers object.");
                return answers.Clone();
            }

            if (attempt >= maxAttempts || !IsRetryable(response.StatusCode))
                throw new HttpRequestException($"TypeSafe returned HTTP {(int)response.StatusCode}.");

            var retryAfter = response.Headers.RetryAfter?.Delta;
            var wait = retryAfter is { } delta && delta > TimeSpan.Zero ? delta : TimeSpan.FromMilliseconds(delayMs);
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            delayMs *= 2;
        }
    }

    private static bool IsRetryable(HttpStatusCode status)
        => status == HttpStatusCode.TooManyRequests || (int)status == OverloadedStatusCode || (int)status >= 500;
}
