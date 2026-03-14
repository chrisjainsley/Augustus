namespace Augustus;

using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public partial class APISimulator
{
    internal class FileManager
    {
        private static readonly JsonSerializerOptions CacheSerializerOptions = new() { WriteIndented = true };

        private string cacheFolderPath;
        private ConcurrentDictionary<string, byte> _touchedHashes = new();
        private string? _currentTestContext;

        public FileManager(string cacheFolderPath)
        {
            this.cacheFolderPath = cacheFolderPath;
            EnsureCacheFolderExists();
        }

        /// <summary>
        /// Gets the effective cache path. When a test context is set, returns a subdirectory
        /// named after the context; otherwise returns the base cache folder path.
        /// </summary>
        internal string CurrentCachePath =>
            _currentTestContext != null
                ? Path.Combine(cacheFolderPath, SanitizeFolderName(_currentTestContext))
                : cacheFolderPath;

        /// <summary>
        /// Sets the current test context, routing cache operations to a subdirectory.
        /// </summary>
        /// <param name="testName">The test or scenario name to use as the subdirectory.</param>
        public void SetTestContext(string testName)
        {
            if (string.IsNullOrWhiteSpace(testName))
                throw new ArgumentException("Test name cannot be null or whitespace.", nameof(testName));
            _currentTestContext = testName;
            _touchedHashes.Clear();
            var contextPath = CurrentCachePath;
            if (!Directory.Exists(contextPath))
            {
                Directory.CreateDirectory(contextPath);
            }
        }

        /// <summary>
        /// Clears the current test context. Runs scoped stale entry removal for the
        /// context's subdirectory before clearing.
        /// </summary>
        public void ClearTestContext()
        {
            if (_currentTestContext != null)
            {
                RemoveStaleEntriesFromPath(CurrentCachePath);
                _currentTestContext = null;
                _touchedHashes.Clear();
            }
        }

        /// <summary>
        /// Sanitizes a string for use as a folder name by replacing invalid path characters
        /// with underscores and collapsing whitespace.
        /// </summary>
        internal static string SanitizeFolderName(string name)
        {
            // Use a fixed cross-platform set so cache folder names are consistent across OSes.
            // On Unix, Path.GetInvalidFileNameChars() only returns '/' and '\0', but characters
            // like ':', '<', '>', '"', '\' are problematic for Windows and should always be sanitized.
            var invalidChars = new HashSet<char>(Path.GetInvalidFileNameChars())
            {
                '\\', '/', ':', '*', '?', '"', '<', '>', '|'
            };
            var sanitized = new char[name.Length];
            for (int i = 0; i < name.Length; i++)
            {
                sanitized[i] = invalidChars.Contains(name[i]) ? '_' : name[i];
            }

            // Replace spaces with underscores and collapse consecutive underscores
            var result = new string(sanitized).Replace(' ', '_');
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }

            result = result.Trim('_');

            // Guard against path traversal: reject "." and ".." as final names
            if (result is "" or "." or "..")
                return "_";

            return result;
        }

        /// <summary>
        /// Changes the base cache folder path and ensures the directory exists.
        /// </summary>
        /// <param name="newBasePath">The new base path for cache storage.</param>
        public void SetCacheBasePath(string newBasePath)
        {
            if (string.IsNullOrWhiteSpace(newBasePath))
                throw new ArgumentException("Cache base path cannot be null or whitespace.", nameof(newBasePath));
            cacheFolderPath = newBasePath;
            _touchedHashes.Clear();
            EnsureCacheFolderExists();

            // If a test context is active, ensure its subdirectory also exists under the new base
            if (_currentTestContext != null)
            {
                var contextPath = CurrentCachePath;
                if (!Directory.Exists(contextPath))
                {
                    Directory.CreateDirectory(contextPath);
                }
            }
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
            string fullPath = Path.Combine(CurrentCachePath, filename);
            await File.WriteAllTextAsync(fullPath, content).ConfigureAwait(false);
        }

        public async Task<string?> ReadFromFileAsync(string filename)
        {
            string fullPath = Path.Combine(CurrentCachePath, filename);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        }

        public async Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions)
        {
            ValidateFileName(requestHash);

            var cacheEntry = new CacheEntry
            {
                RequestHash = requestHash,
                Response = response,
                OriginalRequest = SensitiveDataSanitizer.SanitizeSensitiveValues(originalRequest),
                Instructions = instructions.Select(SensitiveDataSanitizer.SanitizeSensitiveValues).ToList(),
                Timestamp = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(cacheEntry, CacheSerializerOptions);
            await WriteToFileAsync($"{requestHash}.json", json).ConfigureAwait(false);
            _touchedHashes.TryAdd(requestHash, 0);
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
                if (cacheEntry?.Response != null)
                    _touchedHashes.TryAdd(requestHash, 0);
                return cacheEntry?.Response;
            }
            catch (JsonException)
            {
                // Invalid cache file, return null
                return null;
            }
        }

        public void RemoveStaleEntries()
        {
            RemoveStaleEntriesFromPath(cacheFolderPath);
        }

        private void RemoveStaleEntriesFromPath(string path)
        {
            if (!Directory.Exists(path))
                return;

            string[] files;
            try
            {
                files = Directory.GetFiles(path, "*.json");
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            foreach (var file in files)
            {
                var hash = Path.GetFileNameWithoutExtension(file);
                if (!_touchedHashes.ContainsKey(hash))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (FileNotFoundException)
                    {
                    }
                    catch (IOException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not delete stale cache file {file}: {ex.Message}");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Warning: Access denied when deleting stale file {file}: {ex.Message}");
                    }
                }
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
