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

        /// <summary>
        /// <c>true</c> when <see cref="Canonical"/> can be re-hashed to <see cref="Hash"/>
        /// — i.e. the materialized body round-trips losslessly through UTF-8. When
        /// <c>false</c>, persisting the canonical inside the fixture would make the file
        /// unresolvable both by fast-path probe (canonical recomputes to a different key)
        /// and by content index (would map under that wrong key). Handlers must persist
        /// the entry as legacy (filename-keyed, no canonical) in that case.
        /// </summary>
        public bool CanonicalIsRoundtrippable { get; init; }
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

        // When a transform did not touch the body, hash the original materialized bytes so
        // the live key is byte-identical to pre-0.9 Augustus (including non-UTF-8 binary
        // uploads). When the transform did touch the body, hash UTF-8 of the new string so
        // the transform actually changes the key. ComputeKeyFromCanonical always hashes
        // UTF-8 of the stored canonical body — so for the just-written canonical to
        // recompute to the same hash, the materialized body must round-trip losslessly
        // through UTF-8. That holds for every realistic JSON/form payload; for a non-UTF-8
        // binary upload it does not, and CanonicalIsRoundtrippable signals callers to
        // persist the entry as legacy (filename-keyed) instead.
        var bodyChanged = key.NormalizedBody != bodyString;
        byte[] bodyForHash;
        bool canonicalIsRoundtrippable;
        if (bodyChanged)
        {
            bodyForHash = Encoding.UTF8.GetBytes(key.NormalizedBody);
            canonicalIsRoundtrippable = true;
        }
        else
        {
            bodyForHash = materialized;
            canonicalIsRoundtrippable = materialized.Length == 0
                || Encoding.UTF8.GetBytes(bodyString).AsSpan().SequenceEqual(materialized);
        }
        var hash = ComputeHash(canonical, bodyForHash);
        return new Result
        {
            Key = key,
            Canonical = canonical,
            Hash = hash,
            MaterializedBody = materialized,
            CanonicalIsRoundtrippable = canonicalIsRoundtrippable,
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

            // Malformed percent-encoding (e.g. "?bad=%") must not turn cache-key
            // computation into a 500 — fall back to comparing the raw, encoded name.
            string decoded;
            try { decoded = Uri.UnescapeDataString(name); }
            catch (UriFormatException) { decoded = name; }

            if (!ignoreSet.Contains(decoded))
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
