namespace Augustus;

/// <summary>
/// Computes the cache key for a request. Thin façade over <see cref="RequestKeyFactory"/>;
/// the default (no rules) path is byte-identical to the historical key so existing
/// fixtures keep resolving without a rekey.
/// </summary>
internal static class CacheKeyComputer
{
    public static string ComputeCacheKey(string method, string path, string? queryString, byte[] body, List<string>? instructions = null, IEnumerable<string>? dynamicContentFields = null)
    {
        return ComputeCacheKey(method, path, queryString, body, out _, instructions, dynamicContentFields);
    }

    public static string ComputeCacheKey(string method, string path, string? queryString, byte[] body, out byte[] materializedBody, List<string>? instructions = null, IEnumerable<string>? dynamicContentFields = null)
    {
        var fields = dynamicContentFields as IReadOnlyCollection<string>
            ?? (dynamicContentFields != null ? dynamicContentFields.ToArray() : null);

        var rules = fields is { Count: > 0 }
            ? new CacheKeyRules { DynamicContentFields = fields }
            : CacheKeyRules.Legacy;

        var result = RequestKeyFactory.Create(method, path, queryString, body, instructions, rules);
        materializedBody = result.MaterializedBody;
        return result.Hash;
    }
}
