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

        /// <summary>
        /// Mirrors <see cref="APISimulatorOptions.AutoRemoveStaleCache"/> so per-scenario
        /// context cleanup honors the option. Default <c>true</c> preserves historical
        /// behavior for callers that construct a <see cref="FileManager"/> directly.
        /// </summary>
        internal bool AutoRemoveStaleCache { get; set; } = true;

        // Content-addressed index per effective cache path: recomputed key -> file label
        // (filename without extension). Lets renamed / hand-authored / rule-changed fixtures
        // resolve when the fast {key}.json probe misses. Built once per path (fixtures are
        // expected to exist before the first request); our own writes patch it incrementally;
        // dropped on context/base-path changes. Fixtures added externally mid-run are not
        // picked up until the next context switch — edit fixtures between runs.
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _indexByCachePath = new();
        private readonly object _indexLock = new();

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
            InvalidateIndex();
            var contextPath = CurrentCachePath;
            if (!Directory.Exists(contextPath))
            {
                Directory.CreateDirectory(contextPath);
            }
        }

        /// <summary>
        /// Clears the current test context. When <see cref="AutoRemoveStaleCache"/> is
        /// <c>true</c>, runs scoped stale entry removal for the context's subdirectory
        /// before clearing — fixtures not touched during the scenario are deleted on disk.
        /// When <c>false</c>, the on-disk fixtures are preserved verbatim; this is the
        /// right setting for committed __mocks__/ trees where a scenario miss must never
        /// silently delete the file you're trying to debug.
        /// </summary>
        public void ClearTestContext()
        {
            if (_currentTestContext != null)
            {
                if (AutoRemoveStaleCache)
                    RemoveStaleEntriesFromPath(CurrentCachePath);
                _currentTestContext = null;
                _touchedHashes.Clear();
                InvalidateIndex();
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
            InvalidateIndex();
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

        public Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions)
            => CacheResponseAsync(requestHash, response, originalRequest, instructions, normalized: false);

        public async Task CacheResponseAsync(string requestHash, string response, string originalRequest, List<string> instructions, bool normalized, CanonicalRequest? canonical = null)
        {
            ValidateFileName(requestHash);

            var cacheEntry = new CacheEntry
            {
                RequestHash = requestHash,
                Response = response,
                OriginalRequest = SensitiveDataSanitizer.SanitizeSensitiveValues(originalRequest),
                Instructions = instructions.Select(SensitiveDataSanitizer.SanitizeSensitiveValues).ToList(),
                Timestamp = DateTime.UtcNow,
                Normalized = normalized,
                CanonicalRequest = canonical
            };

            var json = JsonSerializer.Serialize(cacheEntry, CacheSerializerOptions);
            await WriteToFileAsync($"{requestHash}.json", json).ConfigureAwait(false);
            _touchedHashes.TryAdd(requestHash, 0);

            // Keep an already-built index consistent within the same process: a freshly
            // written fixture's filename label is its key. Avoids a full rescan after record.
            if (_indexByCachePath.TryGetValue(CurrentCachePath, out var index))
                index[requestHash] = requestHash;
        }

        public async Task<string?> ReadCachedResponseAsync(string requestHash)
        {
            var entry = await ReadCachedEntryAsync(requestHash).ConfigureAwait(false);
            return entry?.Response;
        }

        public async Task<CacheEntry?> ReadCachedEntryAsync(string requestHash)
        {
            ValidateFileName(requestHash);

            var json = await ReadFromFileAsync($"{requestHash}.json");
            var cacheEntry = DeserializeEntry(json);
            if (cacheEntry?.Response != null)
                _touchedHashes.TryAdd(requestHash, 0);
            return cacheEntry;
        }

        /// <summary>
        /// Resolves a cache entry by content: first the fast <c>{primaryKey}.json</c> probe,
        /// then a content-addressed lookup that recomputes each fixture's key from its stored
        /// <see cref="CanonicalRequest"/> via <paramref name="recompute"/>. This is what makes
        /// fixtures renameable / hand-authorable and lets a keying-rule change re-baseline
        /// existing files with zero upstream calls.
        /// </summary>
        public async Task<CacheEntry?> ResolveEntryAsync(string primaryKey, Func<CanonicalRequest, string> recompute)
        {
            var result = await ResolveEntryWithLabelAsync(primaryKey, recompute).ConfigureAwait(false);
            return result?.Entry;
        }

        /// <summary>
        /// Same as <see cref="ResolveEntryAsync"/> but also returns the on-disk file label
        /// (filename without extension) that produced the match. The label is needed by
        /// callers that want to rewrite the fixture in place — e.g.
        /// <see cref="BackfillCanonicalAsync"/> for the
        /// <see cref="APISimulatorOptions.BackfillLegacyCanonicalRequest"/> flow.
        /// </summary>
        public async Task<ResolveResult?> ResolveEntryWithLabelAsync(string primaryKey, Func<CanonicalRequest, string> recompute)
        {
            var direct = await ReadCachedEntryAsync(primaryKey).ConfigureAwait(false);
            if (direct?.Response is { Length: > 0 })
            {
                // For entries that carry a CanonicalRequest, verify it really matches the
                // requested key before serving — otherwise a stale/hand-authored file whose
                // label happens to equal another request's hash would return the wrong
                // response. Legacy entries (null canonical) are trusted by filename for
                // back-compat.
                if (direct.CanonicalRequest is null || recompute(direct.CanonicalRequest) == primaryKey)
                    return new ResolveResult(direct, primaryKey);
            }

            // Built once per effective cache path (fixtures are expected to exist before the
            // first request); our own writes patch it incrementally, context switches drop it.
            var index = GetOrBuildIndex(CurrentCachePath, recompute);
            if (index.TryGetValue(primaryKey, out var label))
            {
                var json = await ReadFromFileAsync($"{label}.json").ConfigureAwait(false);
                var matched = DeserializeEntry(json);
                if (matched?.Response is { Length: > 0 })
                {
                    // Track by the on-disk label so stale-removal keeps renamed fixtures.
                    _touchedHashes.TryAdd(label, 0);
                    return new ResolveResult(matched, label);
                }

                // Label points at a file that was deleted/renamed mid-run: self-heal so the
                // failed probe is not repeated for every subsequent matching request.
                index.TryRemove(primaryKey, out _);
            }

            return null;
        }

        /// <summary>
        /// One-shot in-place rewrite: if the fixture at <paramref name="label"/> has no
        /// <see cref="CanonicalRequest"/>, set it to <paramref name="canonical"/> and
        /// reserialize. Idempotent — a fixture that already carries a CanonicalRequest is
        /// left untouched and the method returns <c>false</c>. The filename is not changed;
        /// use <see cref="CacheMaintenance.Rekey"/> afterwards to apply a new keying rule.
        /// </summary>
        /// <returns><c>true</c> if the file was rewritten, <c>false</c> if it was skipped (idempotent no-op or unreadable).</returns>
        public async Task<bool> BackfillCanonicalAsync(string label, CanonicalRequest canonical)
        {
            ArgumentNullException.ThrowIfNull(canonical);
            ValidateFileName($"{label}.json");

            var json = await ReadFromFileAsync($"{label}.json").ConfigureAwait(false);
            var entry = DeserializeEntry(json);
            if (entry is null || entry.CanonicalRequest is not null)
                return false;

            entry.CanonicalRequest = canonical;
            var updated = JsonSerializer.Serialize(entry, CacheSerializerOptions);
            await WriteToFileAsync($"{label}.json", updated).ConfigureAwait(false);
            return true;
        }

        private static CacheEntry? DeserializeEntry(string? json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<CacheEntry>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private ConcurrentDictionary<string, string> GetOrBuildIndex(
            string path, Func<CanonicalRequest, string> recompute)
        {
            if (_indexByCachePath.TryGetValue(path, out var existing))
                return existing;

            lock (_indexLock)
            {
                if (_indexByCachePath.TryGetValue(path, out existing))
                    return existing;

                var index = new ConcurrentDictionary<string, string>();
                foreach (var file in CacheFiles.JsonFiles(path))
                {
                    var label = Path.GetFileNameWithoutExtension(file);
                    CacheEntry? entry;
                    try
                    {
                        entry = DeserializeEntry(File.ReadAllText(file));
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        continue;
                    }

                    if (entry?.CanonicalRequest is { } canonical)
                    {
                        try
                        {
                            var key = recompute(canonical);
                            // First-wins + warn: silently overwriting would return an
                            // arbitrary response depending on directory-enumeration order.
                            if (!index.TryAdd(key, label) && index[key] != label)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"Warning: cache fixture {file} recomputes to key {key} " +
                                    $"already claimed by {index[key]}.json — skipping. " +
                                    "Remove or re-normalize the duplicate fixture.");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Warning: failed to recompute key for cache file {file}: {ex.Message}");
                        }
                    }
                    else
                    {
                        // Legacy fixture: only resolvable by its original hash filename.
                        index.TryAdd(label, label);
                    }
                }

                _indexByCachePath[path] = index;
                return index;
            }
        }

        private void InvalidateIndex()
        {
            _indexByCachePath.Clear();
        }

        public void RemoveStaleEntries()
        {
            RemoveStaleEntriesFromPath(CurrentCachePath);
        }

        private void RemoveStaleEntriesFromPath(string path)
        {
            foreach (var file in CacheFiles.JsonFiles(path))
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
            ClearCacheFromPath(cacheFolderPath);

            // Also clear any subdirectories (e.g., per-scenario context directories)
            if (Directory.Exists(cacheFolderPath))
            {
                try
                {
                    foreach (var subDir in Directory.GetDirectories(cacheFolderPath))
                    {
                        ClearCacheFromPath(subDir);
                    }
                }
                catch (DirectoryNotFoundException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void ClearCacheFromPath(string path)
        {
            foreach (var file in CacheFiles.JsonFiles(path))
            {
                try
                {
                    File.Delete(file);
                }
                catch (FileNotFoundException) { }
                catch (IOException ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Could not delete cache file {file}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
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
        public bool Normalized { get; set; }

        /// <summary>
        /// The canonical request this fixture matches, written for new entries so the file
        /// can be renamed or hand-authored. Null for legacy fixtures (matched by filename).
        /// </summary>
        public CanonicalRequest? CanonicalRequest { get; set; }
    }

    /// <summary>
    /// Outcome of <see cref="FileManager.ResolveEntryWithLabelAsync"/>: the matched
    /// <see cref="CacheEntry"/> and the on-disk file label (filename without extension)
    /// that produced the match. Use the label for in-place rewrites that must target the
    /// actual fixture file (e.g. legacy <see cref="CanonicalRequest"/> backfill) instead
    /// of the request's computed primary key.
    /// </summary>
    internal sealed record ResolveResult(CacheEntry Entry, string Label);
}
