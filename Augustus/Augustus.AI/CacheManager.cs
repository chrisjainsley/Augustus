namespace Augustus.AI;

using Augustus;
using System.Text.Json;

/// <summary>
/// Manages caching of AI-generated and proxied API responses.
/// </summary>
internal class CacheManager
{
    private readonly string cacheFolderPath;

    public CacheManager(string cacheFolderPath)
    {
        this.cacheFolderPath = cacheFolderPath;
        EnsureCacheFolderExists();
    }

    private void EnsureCacheFolderExists()
    {
        if (!Directory.Exists(cacheFolderPath))
        {
            Directory.CreateDirectory(cacheFolderPath);
        }
    }

    public Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions)
        => CacheResponseAsync(requestHash, response, originalRequest, instructions, normalized: false);

    public async Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions, bool normalized, CanonicalRequest? canonical = null)
    {
        var cacheEntry = new CacheEntry
        {
            RequestHash = requestHash,
            Response = response,
            OriginalRequest = originalRequest,
            Instructions = instructions,
            Timestamp = DateTime.UtcNow,
            Normalized = normalized,
            CanonicalRequest = canonical
        };

        var json = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions { WriteIndented = true });
        string fullPath = Path.Combine(cacheFolderPath, $"{requestHash}.json");
        await File.WriteAllTextAsync(fullPath, json);
    }

    public async Task<string?> ReadCachedResponseAsync(string requestHash)
    {
        string fullPath = Path.Combine(cacheFolderPath, $"{requestHash}.json");
        if (!File.Exists(fullPath))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(fullPath);
            var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(json);
            return cacheEntry?.Response;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void ClearCache()
    {
        if (!Directory.Exists(cacheFolderPath))
            return;

        try
        {
            var files = Directory.GetFiles(cacheFolderPath, "*.json");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete cache file {file}: {ex.Message}");
                }
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Directory was deleted, nothing to do
        }
    }

    /// <summary>
    /// Legacy cache key used by older route-level AI caches (curl + instructions). Prefer <see cref="CacheKeyComputer"/> for new entries.
    /// </summary>
    public static string GenerateLegacyCurlBasedCacheKey(string curlRequest, List<string> instructions)
    {
        var combinedContent = string.Join("|", instructions) + "|" + curlRequest;
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combinedContent));
        return Convert.ToHexString(hash);
    }
}

internal class CacheEntry
{
    public string RequestHash { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public string OriginalRequest { get; set; } = string.Empty;
    public List<string> Instructions { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool Normalized { get; set; }

    /// <summary>
    /// The canonical request this fixture matches, written for new entries so the file
    /// can be renamed or hand-authored. Null for legacy fixtures (matched by filename).
    /// </summary>
    public CanonicalRequest? CanonicalRequest { get; set; }
}
