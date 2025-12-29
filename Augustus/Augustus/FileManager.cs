namespace Augustus;

using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public partial class APISimulator
{
    internal class FileManager
    {
        private readonly string cacheFolderPath;

        public FileManager(string cacheFolderPath)
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

        /// <summary>
        /// Validates that a filename is safe and doesn't contain path traversal characters.
        /// </summary>
        /// <param name="fileName">The filename to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the filename contains invalid characters.</exception>
        private static void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
            }

            // Check for path traversal characters
            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            {
                throw new ArgumentException("File name contains invalid path characters.", nameof(fileName));
            }

            // Check for other invalid filename characters
            var invalidChars = Path.GetInvalidFileNameChars();
            if (fileName.IndexOfAny(invalidChars) >= 0)
            {
                throw new ArgumentException("File name contains invalid characters.", nameof(fileName));
            }
        }

        public async Task WriteToFileAsync(string filename, string content)
        {
            string fullPath = Path.Combine(cacheFolderPath, filename);
            await File.WriteAllTextAsync(fullPath, content);
        }

        public async Task<string?> ReadFromFileAsync(string filename)
        {
            string fullPath = Path.Combine(cacheFolderPath, filename);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllTextAsync(fullPath);
        }

        public async Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions)
        {
            ValidateFileName(requestHash);

            var cacheEntry = new CacheEntry
            {
                RequestHash = requestHash,
                Response = response,
                OriginalRequest = originalRequest,
                Instructions = instructions,
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cacheEntry, new JsonSerializerOptions { WriteIndented = true });
            await WriteToFileAsync($"{requestHash}.json", json);
        }

        public async Task<string?> ReadCachedResponseAsync(string requestHash)
        {
            ValidateFileName(requestHash);

            var json = await ReadFromFileAsync($"{requestHash}.json");
            if (string.IsNullOrEmpty(json))
                return null;

            try
            {
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(json);
                return cacheEntry?.Response;
            }
            catch (JsonException)
            {
                // Invalid cache file, return null
                return null;
            }
        }

        public void ClearCache()
        {
            if (!Directory.Exists(cacheFolderPath))
                return;

            // Get all files first to avoid enumeration-during-modification issues
            string[] files;
            try
            {
                files = Directory.GetFiles(cacheFolderPath, "*.json");
            }
            catch (DirectoryNotFoundException)
            {
                // Directory was deleted between check and enumeration, nothing to do
                return;
            }

            // Delete each file with proper error handling
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (FileNotFoundException)
                {
                    // File already deleted by another thread/process, continue
                }
                catch (IOException ex)
                {
                    // File in use or other I/O error, log and continue with other files
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete cache file {file}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    // Permission issue, log and continue
                    System.Diagnostics.Debug.WriteLine($"Warning: Access denied when deleting {file}: {ex.Message}");
                }
            }
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
}
