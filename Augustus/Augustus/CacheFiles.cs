namespace Augustus;

internal static class CacheFiles
{
    /// <summary>
    /// Enumerates the <c>*.json</c> fixture files in <paramref name="path"/>, returning an
    /// empty array if the directory is absent or vanishes mid-enumeration.
    /// </summary>
    public static string[] JsonFiles(string path)
    {
        if (!Directory.Exists(path))
            return Array.Empty<string>();
        try
        {
            return Directory.GetFiles(path, "*.json");
        }
        catch (DirectoryNotFoundException)
        {
            return Array.Empty<string>();
        }
    }
}
