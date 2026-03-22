using System.Security.Cryptography;
using System.Text;

namespace Augustus;

internal static class CacheKeyComputer
{
    private static readonly byte[] Separator = Encoding.UTF8.GetBytes("|");

    public static string ComputeCacheKey(string method, string path, string? queryString, byte[] body, List<string>? instructions = null, IEnumerable<string>? dynamicContentFields = null)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(method));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(path));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(queryString ?? string.Empty));
        sha.AppendData(Separator);

        var materializedFields = dynamicContentFields as IReadOnlyCollection<string>
            ?? (dynamicContentFields != null ? dynamicContentFields.ToArray() : null);
        var bodyToHash = CacheKeyBodyNormalizer.PrepareBodyForCacheKey(
            body,
            materializedFields ?? (IReadOnlyCollection<string>)Array.Empty<string>());
        sha.AppendData(bodyToHash);
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
}
