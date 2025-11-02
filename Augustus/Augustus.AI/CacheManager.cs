namespace Augustus.AI;

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

    public async Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions)
    {
        var cacheEntry = new CacheEntry
        {
            RequestHash = requestHash,
            Response = response,
            OriginalRequest = originalRequest,
            Instructions = instructions,
            Timestamp = DateTime.UtcNow
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
                catch (Exception ex)
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

    public static string GenerateRequestHash(string curlRequest, List<string> instructions)
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
}
