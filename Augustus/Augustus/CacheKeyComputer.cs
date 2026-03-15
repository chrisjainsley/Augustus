using System.Security.Cryptography;
using System.Text;

namespace Augustus;

internal static class CacheKeyComputer
{
    private static readonly byte[] Separator = Encoding.UTF8.GetBytes("|");

    public static string ComputeCacheKey(string method, string path, string? queryString, byte[] body, List<string>? instructions = null)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(method));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(path));
        sha.AppendData(Separator);
        sha.AppendData(Encoding.UTF8.GetBytes(queryString ?? string.Empty));
        sha.AppendData(Separator);
        sha.AppendData(body);
        if (instructions is { Count: > 0 })
        {
            sha.AppendData(Separator);
            sha.AppendData(Encoding.UTF8.GetBytes(string.Join("|", instructions)));
        }
        return Convert.ToHexString(sha.GetHashAndReset());
    }
}
