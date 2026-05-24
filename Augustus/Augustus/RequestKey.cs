namespace Augustus;

/// <summary>
/// The identity of an HTTP request used for cache matching, after body normalization
/// but before hashing. Supplied to and returned from
/// <see cref="APISimulatorOptions.RequestKeyTransform"/> so consumers can strip or
/// rewrite volatile parts of the request (for example the Azure OpenAI
/// <c>/openai/deployments/{deployment}/…</c> path segment) to keep cache keys stable.
/// </summary>
/// <param name="Method">The HTTP method (e.g. <c>POST</c>).</param>
/// <param name="Path">The request path (e.g. <c>/openai/deployments/gpt-4/chat/completions</c>).</param>
/// <param name="QueryString">The raw query string including the leading <c>?</c>, or <c>null</c>.</param>
/// <param name="Instructions">The AI instructions that participate in the key, in order.</param>
/// <param name="NormalizedBody">
/// The canonicalized request body as a UTF-8 string (sorted JSON keys, normalized
/// newlines, dynamic fields replaced). Empty when there is no body.
/// </param>
/// <remarks>
/// A request-key transform must be pure, deterministic, and idempotent
/// (<c>f(f(x)) == f(x)</c>): it is applied to incoming requests and re-applied to each
/// fixture's already-transformed stored <see cref="CanonicalRequest"/> during content
/// matching, so a non-idempotent transform breaks matching. The built-in
/// <see cref="APISimulatorOptions.NormalizeAzureOpenAIDeployment"/> satisfies this.
/// </remarks>
public sealed record RequestKey(
    string Method,
    string Path,
    string? QueryString,
    IReadOnlyList<string> Instructions,
    string NormalizedBody)
{
    /// <summary>
    /// Compares by value, with <see cref="Instructions"/> compared element-wise so two
    /// identities built from equivalent lists are equal regardless of list instance.
    /// </summary>
    public bool Equals(RequestKey? other) =>
        other is not null
        && Method == other.Method
        && Path == other.Path
        && QueryString == other.QueryString
        && NormalizedBody == other.NormalizedBody
        && Instructions.SequenceEqual(other.Instructions);

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Method, Path, QueryString, NormalizedBody);
        foreach (var s in Instructions)
            hash = HashCode.Combine(hash, s);
        return hash;
    }
}

/// <summary>
/// The canonical request persisted inside each cache file. This is the projection of a
/// <see cref="RequestKey"/> after all normalization and transforms have been applied —
/// i.e. exactly what the cache key is computed from. Augustus matches an incoming request
/// against each fixture's stored <see cref="CanonicalRequest"/>, so the file name is a
/// free-form label rather than the key itself.
/// </summary>
/// <remarks>
/// This is the matching identity and is persisted to the fixture file <em>verbatim</em> —
/// it is not run through sensitive-value redaction (unlike the diagnostic
/// <c>OriginalRequest</c>), because the normalized body/query must round-trip exactly for
/// content matching to work. Keep secrets out of it with <see cref="APISimulatorOptions"/>
/// normalization knobs (<see cref="APISimulatorOptions.DynamicContentFields"/>,
/// <see cref="APISimulatorOptions.IgnoredQueryParameters"/>).
/// </remarks>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The request path.</param>
/// <param name="QueryString">The query string, or <c>null</c>.</param>
/// <param name="Instructions">The AI instructions that participate in the key, in order.</param>
/// <param name="NormalizedBody">The canonicalized request body as a UTF-8 string.</param>
public sealed record CanonicalRequest(
    string Method,
    string Path,
    string? QueryString,
    IReadOnlyList<string> Instructions,
    string NormalizedBody)
{
    /// <summary>
    /// Compares by value, with <see cref="Instructions"/> compared element-wise so two
    /// canonicals built from equivalent lists are equal regardless of list instance.
    /// </summary>
    public bool Equals(CanonicalRequest? other) =>
        other is not null
        && Method == other.Method
        && Path == other.Path
        && QueryString == other.QueryString
        && NormalizedBody == other.NormalizedBody
        && Instructions.SequenceEqual(other.Instructions);

    public override int GetHashCode()
    {
        var hash = HashCode.Combine(Method, Path, QueryString, NormalizedBody);
        foreach (var s in Instructions)
            hash = HashCode.Combine(hash, s);
        return hash;
    }
}

/// <summary>
/// Diagnostic information emitted on a cache miss via
/// <see cref="APISimulatorOptions.OnCacheMiss"/>. Lets a developer discover the exact
/// identity Augustus expected so a fixture can be authored or renamed by hand even when
/// the upstream API is unavailable.
/// </summary>
/// <param name="ExpectedCanonicalRequest">The canonical request Augustus computed for the incoming request.</param>
/// <param name="ComputedKey">The cache key (hex SHA-256) computed from <paramref name="ExpectedCanonicalRequest"/>.</param>
/// <param name="CachePath">The effective cache folder that was searched.</param>
public sealed record CacheMissDiagnostic(
    CanonicalRequest ExpectedCanonicalRequest,
    string ComputedKey,
    string CachePath);
