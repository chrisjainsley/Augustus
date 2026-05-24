using System.Text.Json;

namespace Augustus;

/// <summary>
/// Offline maintenance for committed cache fixtures. Recomputes file names from each
/// fixture's stored <see cref="CanonicalRequest"/> under a (possibly new) keying rule, so
/// adopting a new rule re-baselines fixtures with zero upstream API calls.
/// </summary>
public static class CacheMaintenance
{
    /// <summary>
    /// Recomputes the cache key for every fixture in <paramref name="cachePath"/> from its
    /// stored <see cref="CanonicalRequest"/> and renames the file to <c>{key}.json</c>.
    /// Files without a <see cref="CanonicalRequest"/> are skipped (their identity is
    /// unknowable). Collisions are reported and never overwritten.
    /// </summary>
    /// <param name="cachePath">The cache folder to rekey.</param>
    /// <param name="options">Keying rules and behavior (recursion, dry-run).</param>
    /// <remarks>
    /// Renames are two-phase per directory (every planned file moves to a unique temp name
    /// first, then to its final key), so chains and cycles like <c>A→B, B→A</c> resolve in
    /// a single pass. N→1 collisions (two fixtures recomputing to the same key) and
    /// unreadable files are reported in <see cref="RekeyResult.Conflicts"/> /
    /// <see cref="RekeyResult.Skipped"/> and never overwritten.
    /// </remarks>
    public static RekeyResult Rekey(string cachePath, RekeyOptions options)
    {
        ArgumentNullException.ThrowIfNull(cachePath);
        ArgumentNullException.ThrowIfNull(options);

        var rules = options.ToCacheKeyRules();
        var scanned = 0;
        var renamed = 0;
        var skipped = 0;
        var conflicts = new List<string>();

        foreach (var dir in EnumerateDirectories(cachePath, options.Recursive))
        {
            var files = CacheFiles.JsonFiles(dir);

            // Plan renames first so collisions can be detected without mutating the disk.
            var plan = new List<(string source, string targetName)>();
            var targetCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            foreach (var file in files)
            {
                scanned++;
                APISimulator.CacheEntry? entry;
                try
                {
                    entry = JsonSerializer.Deserialize<APISimulator.CacheEntry>(File.ReadAllText(file));
                }
                catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
                {
                    skipped++;
                    continue;
                }

                if (entry?.CanonicalRequest is not { } canonical)
                {
                    skipped++;
                    continue;
                }

                var targetName = RequestKeyFactory.ComputeKeyFromCanonical(canonical, rules) + ".json";
                plan.Add((file, targetName));
                targetCounts[targetName] = targetCounts.GetValueOrDefault(targetName) + 1;
            }

            // Filter the plan: drop already-correctly-keyed files and N→1 collisions.
            var moves = new List<(string source, string targetName)>();
            foreach (var (source, targetName) in plan)
            {
                if (string.Equals(Path.GetFileName(source), targetName, StringComparison.Ordinal))
                    continue; // already correctly keyed

                if (targetCounts[targetName] > 1)
                {
                    conflicts.Add(source);
                    continue;
                }

                moves.Add((source, targetName));
            }

            // A planned target is only blocked if some existing file occupies its slot and
            // isn't itself being moved away — predict that without touching disk so DryRun
            // reports honestly and the two-phase rename has no surprises.
            var plannedSourceNames = new HashSet<string>(
                moves.Select(m => Path.GetFileName(m.source)!), StringComparer.Ordinal);

            // Two-phase rename so chains and cycles (A→B, B→A) resolve in a single pass:
            // every move first goes to a unique temp name, then to its final key.
            var phase1 = new List<(string tempPath, string targetName, string sourceForReporting)>();
            foreach (var (source, targetName) in moves)
            {
                var target = Path.Combine(dir, targetName);
                if (File.Exists(target) && !plannedSourceNames.Contains(targetName))
                {
                    conflicts.Add(source);
                    continue;
                }

                var temp = Path.Combine(dir, $"{Guid.NewGuid():N}.rekey.tmp");
                if (!options.DryRun)
                {
                    try
                    {
                        File.Move(source, temp, overwrite: false);
                    }
                    catch (IOException)
                    {
                        conflicts.Add(source);
                        continue;
                    }
                }
                phase1.Add((temp, targetName, source));
            }

            foreach (var (tempPath, targetName, sourceForReporting) in phase1)
            {
                if (!options.DryRun)
                {
                    try
                    {
                        File.Move(tempPath, Path.Combine(dir, targetName), overwrite: false);
                    }
                    catch (IOException)
                    {
                        // Don't strand the fixture as *.rekey.tmp (invisible to *.json
                        // scans on the next run): best-effort revert temp -> source. If
                        // revert also fails, surface the temp path so the caller can
                        // recover manually instead of silently losing the file.
                        try
                        {
                            File.Move(tempPath, sourceForReporting, overwrite: false);
                            conflicts.Add(sourceForReporting);
                        }
                        catch (IOException)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"Warning: Rekey could not revert {tempPath} to {sourceForReporting}; " +
                                "manual recovery required.");
                            conflicts.Add(tempPath);
                        }
                        continue;
                    }
                }

                renamed++;
            }
        }

        return new RekeyResult(scanned, renamed, skipped, conflicts);
    }

    private static IEnumerable<string> EnumerateDirectories(string root, bool recursive)
    {
        if (!Directory.Exists(root))
            yield break;

        yield return root;
        if (!recursive)
            yield break;

        foreach (var sub in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            yield return sub;
    }
}

/// <summary>
/// Options controlling <see cref="CacheMaintenance.Rekey"/>. The keying rules must match
/// the rules the simulator will use at run time for the rekey to be meaningful.
/// </summary>
public sealed class RekeyOptions
{
    private readonly List<string> _dynamicContentFields = new();
    private readonly List<string> _ignoredQueryParameters = new();

    /// <summary>Mirrors <see cref="APISimulatorOptions.RequestKeyTransform"/>.</summary>
    public Func<RequestKey, RequestKey>? RequestKeyTransform { get; set; }

    /// <summary>Mirrors <see cref="APISimulatorOptions.NormalizeAzureOpenAIDeployment"/>.</summary>
    public bool NormalizeAzureOpenAIDeployment { get; set; }

    /// <summary>Mirrors <see cref="APISimulatorOptions.DynamicContentFields"/>.</summary>
    public IList<string> DynamicContentFields => _dynamicContentFields;

    /// <summary>Mirrors <see cref="APISimulatorOptions.IgnoredQueryParameters"/>.</summary>
    public IList<string> IgnoredQueryParameters => _ignoredQueryParameters;

    /// <summary>Mirrors <see cref="APISimulatorOptions.StripNullBodyProperties"/>.</summary>
    public bool StripNullBodyProperties { get; set; }

    /// <summary>Mirrors <see cref="APISimulatorOptions.HashMessagesContentOnly"/>.</summary>
    public bool HashMessagesContentOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether per-context subdirectories are also rekeyed.
    /// Default is <c>true</c>.
    /// </summary>
    public bool Recursive { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to report what would change without
    /// renaming any files. Default is <c>false</c>.
    /// </summary>
    public bool DryRun { get; set; }

    internal CacheKeyRules ToCacheKeyRules() => CacheKeyRules.From(
        _dynamicContentFields,
        _ignoredQueryParameters,
        RequestKeyTransform,
        NormalizeAzureOpenAIDeployment,
        StripNullBodyProperties,
        HashMessagesContentOnly);
}

/// <summary>
/// The outcome of a <see cref="CacheMaintenance.Rekey"/> run.
/// </summary>
/// <param name="Scanned">Total fixture files inspected.</param>
/// <param name="Renamed">Files renamed to their recomputed key.</param>
/// <param name="Skipped">Files skipped (no <see cref="CanonicalRequest"/> or unreadable).</param>
/// <param name="Conflicts">Source paths left untouched because renaming would overwrite another fixture.</param>
public sealed record RekeyResult(
    int Scanned,
    int Renamed,
    int Skipped,
    IReadOnlyList<string> Conflicts);
