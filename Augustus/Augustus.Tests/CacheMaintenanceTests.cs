using System.Text;
using System.Text.Json;
using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class CacheMaintenanceTests : IDisposable
{
    private readonly List<string> _dirs = new();

    private string NewDir()
    {
        var d = Path.Combine(Path.GetTempPath(), $"augustus-rekey-{Guid.NewGuid():N}");
        Directory.CreateDirectory(d);
        _dirs.Add(d);
        return d;
    }

    public void Dispose()
    {
        foreach (var d in _dirs)
            try { Directory.Delete(d, true); } catch { }
    }

    private static async Task WriteFixtureAsync(string dir, string fileName, CanonicalRequest canonical, string response)
    {
        var json = JsonSerializer.Serialize(new
        {
            RequestHash = "irrelevant",
            Response = response,
            OriginalRequest = "",
            Instructions = Array.Empty<string>(),
            Timestamp = DateTime.UtcNow,
            Normalized = false,
            CanonicalRequest = canonical
        });
        await File.WriteAllTextAsync(Path.Combine(dir, fileName), json);
    }

    private static CanonicalRequest CanonicalFor(string method, string path, string? query, string body)
        => RequestKeyFactory.Create(method, path, query, Encoding.UTF8.GetBytes(body), null, null).Canonical;

    [Fact]
    public async Task Rekey_RecomputesFilenamesFromCanonicalRequest()
    {
        var dir = NewDir();
        var canonical = CanonicalFor("POST", "/v1/chat", null, "{\"model\":\"gpt-4\"}");
        await WriteFixtureAsync(dir, "hand-named.json", canonical, "{\"ok\":1}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions());

        var expectedKey = RequestKeyFactory.ComputeKeyFromCanonical(canonical, null);
        result.Scanned.Should().Be(1);
        result.Renamed.Should().Be(1);
        File.Exists(Path.Combine(dir, $"{expectedKey}.json")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "hand-named.json")).Should().BeFalse();
    }

    [Fact]
    public async Task Rekey_AppliesAzureNormalization_RenamesDeploymentAgnostic()
    {
        var dir = NewDir();
        var canonical = CanonicalFor("POST", "/openai/deployments/prod/chat/completions", "?api-version=2024-06-01", "{\"model\":\"gpt-4\"}");
        await WriteFixtureAsync(dir, "old.json", canonical, "{\"ok\":2}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions { NormalizeAzureOpenAIDeployment = true });

        var normRules = new RekeyOptions { NormalizeAzureOpenAIDeployment = true };
        var expectedKey = RequestKeyFactory.ComputeKeyFromCanonical(canonical, normRules.ToCacheKeyRules());
        result.Renamed.Should().Be(1);
        File.Exists(Path.Combine(dir, $"{expectedKey}.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Rekey_DryRun_DoesNotRename()
    {
        var dir = NewDir();
        var canonical = CanonicalFor("GET", "/v1/x", null, "");
        await WriteFixtureAsync(dir, "keep-me.json", canonical, "{\"ok\":3}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions { DryRun = true });

        result.Renamed.Should().Be(1); // would-rename count
        File.Exists(Path.Combine(dir, "keep-me.json")).Should().BeTrue();
        Directory.GetFiles(dir, "*.json").Should().ContainSingle();
    }

    [Fact]
    public async Task Rekey_Conflict_ReportedAndNotOverwritten()
    {
        var dir = NewDir();
        var canonical = CanonicalFor("POST", "/v1/chat", null, "{\"model\":\"gpt-4\"}");
        await WriteFixtureAsync(dir, "first.json", canonical, "{\"ok\":\"a\"}");
        await WriteFixtureAsync(dir, "second.json", canonical, "{\"ok\":\"b\"}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions());

        result.Conflicts.Should().HaveCount(2);
        result.Renamed.Should().Be(0);
        File.Exists(Path.Combine(dir, "first.json")).Should().BeTrue();
        File.Exists(Path.Combine(dir, "second.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Rekey_LegacyFileWithoutCanonicalRequest_Skipped()
    {
        var dir = NewDir();
        var legacy =
            "{\"RequestHash\":\"LEGACY\",\"Response\":\"{}\",\"OriginalRequest\":\"\"," +
            "\"Instructions\":[],\"Timestamp\":\"2024-01-01T00:00:00Z\",\"Normalized\":true}";
        await File.WriteAllTextAsync(Path.Combine(dir, "LEGACY.json"), legacy);

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions());

        result.Scanned.Should().Be(1);
        result.Skipped.Should().Be(1);
        result.Renamed.Should().Be(0);
        File.Exists(Path.Combine(dir, "LEGACY.json")).Should().BeTrue();
    }

    [Fact]
    public async Task Rekey_KeyCycle_ResolvesInOnePassViaTwoPhaseRename()
    {
        var dir = NewDir();
        var canonA = CanonicalFor("POST", "/v1/a", null, "{}");
        var canonB = CanonicalFor("POST", "/v1/b", null, "{}");
        var keyA = RequestKeyFactory.ComputeKeyFromCanonical(canonA, null);
        var keyB = RequestKeyFactory.ComputeKeyFromCanonical(canonB, null);
        keyA.Should().NotBe(keyB);

        // Cross-keyed: file named {keyB}.json holds canonA, file named {keyA}.json holds canonB.
        await WriteFixtureAsync(dir, $"{keyB}.json", canonA, "{\"r\":\"a\"}");
        await WriteFixtureAsync(dir, $"{keyA}.json", canonB, "{\"r\":\"b\"}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions());

        result.Renamed.Should().Be(2);
        result.Conflicts.Should().BeEmpty();

        // Each fixture is now at the file matching its canonical's recomputed key.
        var atKeyA = JsonSerializer.Deserialize<APISimulator.CacheEntry>(
            await File.ReadAllTextAsync(Path.Combine(dir, $"{keyA}.json")));
        var atKeyB = JsonSerializer.Deserialize<APISimulator.CacheEntry>(
            await File.ReadAllTextAsync(Path.Combine(dir, $"{keyB}.json")));
        atKeyA!.Response.Should().Be("{\"r\":\"a\"}");
        atKeyB!.Response.Should().Be("{\"r\":\"b\"}");

        // No temp files left behind.
        Directory.GetFiles(dir, "*.rekey.tmp").Should().BeEmpty();
    }

    [Fact]
    public async Task Rekey_Recursive_ProcessesPerContextSubdirectories()
    {
        var dir = NewDir();
        var sub = Path.Combine(dir, "MyScenario");
        Directory.CreateDirectory(sub);
        var canonical = CanonicalFor("GET", "/v1/sub", null, "");
        await WriteFixtureAsync(sub, "renamed-me.json", canonical, "{\"ok\":4}");

        var result = CacheMaintenance.Rekey(dir, new RekeyOptions { Recursive = true });

        var expectedKey = RequestKeyFactory.ComputeKeyFromCanonical(canonical, null);
        result.Renamed.Should().Be(1);
        File.Exists(Path.Combine(sub, $"{expectedKey}.json")).Should().BeTrue();
    }
}
