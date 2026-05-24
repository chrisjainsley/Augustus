using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Augustus;

/// <summary>
/// Normalization rules applied when computing a cache key. The default instance
/// (<see cref="Legacy"/>) reproduces the historical key byte-for-byte so existing
/// fixtures keep resolving without a rekey.
/// </summary>
internal sealed class CacheKeyRules
{
    public IReadOnlyCollection<string>? DynamicContentFields { get; init; }
    public IReadOnlyCollection<string>? IgnoredQueryParameters { get; init; }
    public bool NormalizeAzureOpenAIDeployment { get; init; }
    public Func<RequestKey, RequestKey>? RequestKeyTransform { get; init; }
    public bool StripNullBodyProperties { get; init; }
    public bool HashMessagesContentOnly { get; init; }

    public static readonly CacheKeyRules Legacy = new();

    /// <summary>
    /// Single projection used by both <see cref="APISimulatorOptions"/> and
    /// <see cref="RekeyOptions"/> so the simulator and the offline rekey tool always derive
    /// identical rules from the same settings.
    /// </summary>
    public static CacheKeyRules From(
        IReadOnlyList<string> dynamicContentFields,
        IReadOnlyList<string> ignoredQueryParameters,
        Func<RequestKey, RequestKey>? requestKeyTransform,
        bool normalizeAzureOpenAIDeployment,
        bool stripNullBodyProperties,
        bool hashMessagesContentOnly) => new()
    {
        DynamicContentFields = dynamicContentFields.Count > 0 ? dynamicContentFields.ToArray() : null,
        IgnoredQueryParameters = ignoredQueryParameters.Count > 0 ? ignoredQueryParameters.ToArray() : null,
        RequestKeyTransform = requestKeyTransform,
        NormalizeAzureOpenAIDeployment = normalizeAzureOpenAIDeployment,
        StripNullBodyProperties = stripNullBodyProperties,
        HashMessagesContentOnly = hashMessagesContentOnly,
    };
}

/// <summary>
/// Single canonicalization path shared by the request handlers, the content index, and
/// the offline rekey API. Produces the <see cref="RequestKey"/>, its persisted
/// <see cref="CanonicalRequest"/> projection, and the cache key (hex SHA-256).
/// </summary>
internal static class RequestKeyFactory
{
    private static readonly byte[] Separator = Encoding.UTF8.GetBytes("|");

    internal readonly struct Result
    {
        public RequestKey Key { get; init; }
        public CanonicalRequest Canonical { get; init; }
        public string Hash { get; init; }
        public byte[] MaterializedBody { get; init; }
    }

    public static Result Create(
        string method,
        string path,
        string? queryString,
        byte[] body,
        IReadOnlyList<string>? instructions,
        CacheKeyRules? rules)
    {
        rules ??= CacheKeyRules.Legacy;

        var fields = rules.DynamicContentFields ?? Array.Empty<string>();
        var materialized = CacheKeyBodyNormalizer.PrepareBodyForCacheKey(
            body,
            fields,
            rules.StripNullBodyProperties,
            rules.HashMessagesContentOnly);

        var bodyString = Encoding.UTF8.GetString(materialized);
        var effectiveQuery = StripIgnoredQueryParameters(queryString, rules.IgnoredQueryParameters);
        var instructionList = instructions ?? Array.Empty<string>();

        var key = new RequestKey(method, path, effectiveQuery, instructionList, bodyString);

        if (rules.NormalizeAzureOpenAIDeployment)
            key = AzureOpenAINormalization.Apply(key);

        if (rules.RequestKeyTransform is { } transform)
            key = transform(key) ?? key;

        var canonical = new CanonicalRequest(
            key.Method, key.Path, key.QueryString, key.Instructions, key.NormalizedBody);

        // Hash the canonical body string the same way ComputeKeyFromCanonical does so a
        // just-written fixture resolves byte-identically when later matched via its stored
        // CanonicalRequest (the invariant the renameable-fixture feature depends on).
        // For UTF-8 bodies — JSON and form-encoded, i.e. every realistic API request body —
        // this round-trips exactly and equals the historical key. A request body that is
        // not valid UTF-8 (raw binary upload) hashes differently than pre-0.9 Augustus and
        // is resolvable only by its hash filename, not by content; this is an accepted,
        // documented edge for an API simulator.
        var hash = ComputeHash(canonical, Encoding.UTF8.GetBytes(canonical.NormalizedBody));
        return new Result
        {
            Key = key,
            Canonical = canonical,
            Hash = hash,
            MaterializedBody = materialized,
        };
    }

    /// <summary>
    /// Recomputes the key from a fixture's stored <see cref="CanonicalRequest"/>, applying the
    /// same <paramref name="rules"/> as live requests so a keying-rule change re-baselines
    /// existing fixtures with zero upstream calls.
    /// </summary>
    public static string ComputeKeyFromCanonical(CanonicalRequest canonical, CacheKeyRules? rules)
    {
        rules ??= CacheKeyRules.Legacy;

        var key = new RequestKey(
            canonical.Method, canonical.Path, canonical.QueryString,
            canonical.Instructions, canonical.NormalizedBody);

        if (rules.NormalizeAzureOpenAIDeployment)
            key = AzureOpenAINormalization.Apply(key);

        if (rules.RequestKeyTransform is { } transform)
            key = transform(key) ?? key;

        var transformed = new CanonicalRequest(
            key.Method, key.Path, key.QueryString, key.Instructions, key.NormalizedBody);

        return ComputeHash(transformed, Encoding.UTF8.GetBytes(transformed.NormalizedBody));
    }

    internal static string ComputeHash(CanonicalRequest canonical, byte[] bodyBytes)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(canonical.Method));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(canonical.Path));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(canonical.QueryString ?? string.Empty));
        sha.AppendData(Separator);
        sha.AppendData(bodyBytes);

        var instructions = canonical.Instructions;
        if (instructions is { Count: > 0 })
        {
            sha.AppendData(Separator);
            sha.AppendData(Encoding.UTF8.GetBytes(instructions.Count.ToString()));
            foreach (var instruction in instructions)
            {
                sha.AppendData(Separator);
                sha.AppendData(Encoding.UTF8.GetBytes(instruction));
            }
        }

        return Convert.ToHexString(sha.GetHashAndReset());
    }

    private static string? StripIgnoredQueryParameters(string? queryString, IReadOnlyCollection<string>? ignored)
    {
        if (ignored is null || ignored.Count == 0 || string.IsNullOrEmpty(queryString))
            return queryString;

        var ignoreSet = new HashSet<string>(ignored, StringComparer.OrdinalIgnoreCase);
        var trimmed = queryString.StartsWith('?') ? queryString[1..] : queryString;
        if (trimmed.Length == 0)
            return queryString;

        var kept = new List<string>();
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            var name = eq >= 0 ? pair[..eq] : pair;
            if (!ignoreSet.Contains(Uri.UnescapeDataString(name)))
                kept.Add(pair);
        }

        return kept.Count == 0 ? string.Empty : "?" + string.Join("&", kept);
    }
}

/// <summary>
/// Built-in request-key transform that replaces the volatile Azure OpenAI deployment
/// path segment AND the top-level <c>"model"</c> field in the JSON request body with
/// a constant so a deployment or region rename does not invalidate committed fixtures.
/// Both substitutions are necessary because the deployment name appears in TWO places
/// in a typical Azure OpenAI chat-completion request: the URL path
/// (<c>/openai/deployments/{deployment}/chat/completions</c>) and the body's top-level
/// <c>model</c> property, which the OpenAI SDK populates from the deployment name.
/// </summary>
internal static class AzureOpenAINormalization
{
    private const string DeploymentPlaceholder = "__DEPLOYMENT__";
    private const string ModelFieldName = "model";

    private static readonly Regex DeploymentSegment = new(
        @"(?<prefix>/openai/deployments/)(?<deployment>[^/]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Must match the encoder used by CacheKeyBodyNormalizer so a re-serialized body
    // hashes byte-identically to the originally normalized body.
    private static readonly JsonSerializerOptions BodySerializerOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static RequestKey Apply(RequestKey key)
    {
        var newPath = NormalizePath(key.Path);
        var newBody = NormalizeBodyModelField(key.NormalizedBody);

        if (ReferenceEquals(newPath, key.Path) && ReferenceEquals(newBody, key.NormalizedBody))
            return key;

        return key with { Path = newPath, NormalizedBody = newBody };
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !DeploymentSegment.IsMatch(path))
            return path;

        var replaced = DeploymentSegment.Replace(path, "${prefix}" + DeploymentPlaceholder);
        return replaced == path ? path : replaced;
    }

    private static string NormalizeBodyModelField(string body)
    {
        // Fast bail-out — avoid parsing for non-JSON or bodies that obviously don't
        // contain a top-level "model" property.
        if (string.IsNullOrEmpty(body) || !body.Contains("\"" + ModelFieldName + "\""))
            return body;

        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(body);
            if (node is System.Text.Json.Nodes.JsonObject obj && obj.ContainsKey(ModelFieldName))
            {
                // System.Text.Json.Nodes.JsonObject preserves insertion order. The canonical
                // body's keys were sorted alphabetically by CacheKeyBodyNormalizer.SortKeysRecursive,
                // so mutating in place keeps that order — serialization round-trips at the same hash.
                obj[ModelFieldName] = DeploymentPlaceholder;
                return obj.ToJsonString(BodySerializerOptions);
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Body looked like JSON but wasn't — leave it alone.
        }

        return body;
    }
}
