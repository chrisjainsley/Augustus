using FluentAssertions;
using System.Text;
using System.Text.Json;

namespace Augustus.Tests;

/// <summary>
/// Validates the legacy-fixture backfill path added on top of PR #106 so a pre-canonical
/// fixture (Augustus 0.8.0 and earlier) can gain a <c>CanonicalRequest</c> without any
/// upstream API call — making it eligible for <see cref="CacheMaintenance.Rekey"/>.
/// </summary>
public class CanonicalBackfillTests
{
    private static readonly Func<CanonicalRequest, string> Recompute =
        c => RequestKeyFactory.ComputeKeyFromCanonical(c, null);

    private static (string key, CanonicalRequest canonical) BuildKey(
        string method, string path, string? query, byte[] body)
    {
        var result = RequestKeyFactory.Create(method, path, query, body, null, null);
        return (result.Hash, result.Canonical);
    }

    private static async Task WriteLegacyFixtureAsync(string cachePath, string label, string response)
    {
        var legacyJson =
            "{\"RequestHash\":\"" + label + "\"," +
            "\"Response\":" + JsonSerializer.Serialize(response) + "," +
            "\"OriginalRequest\":\"\",\"Instructions\":[]," +
            "\"Timestamp\":\"2024-01-01T00:00:00Z\",\"Normalized\":true}";
        await File.WriteAllTextAsync(Path.Combine(cachePath, $"{label}.json"), legacyJson);
    }

    [Fact]
    public async Task BackfillCanonicalAsync_OnLegacyFile_WritesCanonicalAndEnablesRekey()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-backfill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (legacyKey, canonical) = BuildKey("POST", "/openai/deployments/old-dev/chat/completions", null, body);

            // Seed a legacy fixture under the legacy hash filename, with NO CanonicalRequest.
            await WriteLegacyFixtureAsync(cachePath, legacyKey, "{\"ok\":\"legacy\"}");

            // Sanity: legacy file deserializes but has no CanonicalRequest.
            var fmCheck = new APISimulator.FileManager(cachePath);
            var pre = await fmCheck.ReadCachedEntryAsync(legacyKey);
            pre.Should().NotBeNull();
            pre!.CanonicalRequest.Should().BeNull();

            // Act: backfill in place.
            var fm = new APISimulator.FileManager(cachePath);
            var wrote = await fm.BackfillCanonicalAsync(legacyKey, canonical);

            // Assert: file gained a CanonicalRequest, response untouched.
            wrote.Should().BeTrue();
            var post = await fm.ReadCachedEntryAsync(legacyKey);
            post.Should().NotBeNull();
            post!.CanonicalRequest.Should().NotBeNull();
            post.Response.Should().Be("{\"ok\":\"legacy\"}");

            // And: CacheMaintenance.Rekey now treats it as a first-class fixture (no longer skipped).
            // Rekeying with the same rules used to build the key is a no-op rename (same filename),
            // so we use a normalization knob to force a rename and prove Rekey processed the file.
            var rekeyResult = CacheMaintenance.Rekey(cachePath, new RekeyOptions
            {
                NormalizeAzureOpenAIDeployment = true,
                Recursive = false,
                DryRun = false,
            });

            rekeyResult.Skipped.Should().Be(0, "the backfilled CanonicalRequest makes Rekey able to process the file");
            rekeyResult.Conflicts.Should().BeEmpty();
            rekeyResult.Renamed.Should().Be(1, "normalization changed the key, so the file should be renamed");
            File.Exists(Path.Combine(cachePath, $"{legacyKey}.json")).Should().BeFalse("file was renamed under the new key");
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task BackfillCanonicalAsync_AlreadyHasCanonical_IsIdempotentNoOp()
    {
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-backfill-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            var body = Encoding.UTF8.GetBytes("{\"model\":\"gpt-4\"}");
            var (key, canonical) = BuildKey("POST", "/v1/chat/completions", null, body);

            // Seed a *new-format* fixture (already has CanonicalRequest).
            var fm = new APISimulator.FileManager(cachePath);
            await fm.CacheResponseAsync(key, "{\"ok\":1}", "curl", new List<string>(), true, canonical);

            var fullPath = Path.Combine(cachePath, $"{key}.json");
            var beforeMtime = File.GetLastWriteTimeUtc(fullPath);

            // Force a perceptible mtime gap on filesystems with second-level granularity.
            await Task.Delay(1100);

            // Act: backfill with the same canonical it already has.
            var wrote = await fm.BackfillCanonicalAsync(key, canonical);

            // Assert: no write happened.
            wrote.Should().BeFalse("BackfillCanonicalAsync is idempotent when CanonicalRequest is already present");
            File.GetLastWriteTimeUtc(fullPath).Should().Be(beforeMtime, "file should not have been rewritten");
        }
        finally { Directory.Delete(cachePath, true); }
    }

    [Fact]
    public async Task BackfillLegacyCanonicalRequest_DefaultOff_LegacyFilePreservedByteIdentical()
    {
        // This guards the "default behavior is byte-identical to before" promise of PR #106
        // even with the new backfill feature in the codebase: an APISimulatorOptions with
        // the default value (false) for BackfillLegacyCanonicalRequest must not trigger any
        // mutation of legacy fixtures during normal lookups. We assert at the option-default
        // level here; integration coverage that exercises the handler path lives in
        // RefineAI's Server.Specs (Phase 3 of the migration plan).
        var options = new APISimulatorOptions();
        options.BackfillLegacyCanonicalRequest.Should().BeFalse(
            "default must remain off so existing fixtures are byte-identical without opt-in");

        // And: invoking BackfillCanonicalAsync directly is still a controlled op — when the
        // file already lacks a CanonicalRequest and the option is OFF, the handler simply
        // would not call BackfillCanonicalAsync at all (verified by the option guard in
        // ProxyDefaultHandler/AIDefaultHandler). Here we just round-trip read a legacy file
        // through ReadCachedEntryAsync to confirm no in-place migration happens on read.
        var cachePath = Path.Combine(Path.GetTempPath(), $"augustus-backfill-default-off-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cachePath);
        try
        {
            await WriteLegacyFixtureAsync(cachePath, "LEGACYKEY", "{\"ok\":\"legacy\"}");
            var fullPath = Path.Combine(cachePath, "LEGACYKEY.json");
            var beforeBytes = await File.ReadAllBytesAsync(fullPath);

            var fm = new APISimulator.FileManager(cachePath);
            var hit = await fm.ResolveEntryAsync("LEGACYKEY", Recompute);
            hit.Should().NotBeNull();
            hit!.CanonicalRequest.Should().BeNull();

            var afterBytes = await File.ReadAllBytesAsync(fullPath);
            afterBytes.Should().Equal(beforeBytes, "a read with default options must not mutate a legacy fixture on disk");
        }
        finally { Directory.Delete(cachePath, true); }
    }
}
