using System.Text;
using Augustus;
using FluentAssertions;

namespace Augustus.Tests;

public class RequestKeyTransformTests
{
    private static byte[] Body(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void AzureNormalization_DifferentDeployments_ProduceSameKey()
    {
        var rules = new CacheKeyRules { NormalizeAzureOpenAIDeployment = true };

        var prod = RequestKeyFactory.Create(
            "POST", "/openai/deployments/gpt-4-prod/chat/completions", "?api-version=2024-06-01",
            Body("{\"model\":\"gpt-4\"}"), null, rules);
        var dev = RequestKeyFactory.Create(
            "POST", "/openai/deployments/gpt-4-dev-westus/chat/completions", "?api-version=2024-06-01",
            Body("{\"model\":\"gpt-4\"}"), null, rules);

        dev.Hash.Should().Be(prod.Hash);
        prod.Canonical.Path.Should().Be("/openai/deployments/__DEPLOYMENT__/chat/completions");
    }

    [Fact]
    public void AzureNormalization_Disabled_DeploymentRenameChangesKey()
    {
        var a = RequestKeyFactory.Create(
            "POST", "/openai/deployments/a/chat/completions", null, Body("{}"), null, null);
        var b = RequestKeyFactory.Create(
            "POST", "/openai/deployments/b/chat/completions", null, Body("{}"), null, null);

        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void RequestKeyTransform_RewritesPath_AffectsKey()
    {
        var rules = new CacheKeyRules
        {
            RequestKeyTransform = k => k with { Path = "/normalized" }
        };

        var one = RequestKeyFactory.Create("GET", "/v1/a", null, Array.Empty<byte>(), null, rules);
        var two = RequestKeyFactory.Create("GET", "/v1/b", null, Array.Empty<byte>(), null, rules);

        one.Hash.Should().Be(two.Hash);
        one.Canonical.Path.Should().Be("/normalized");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecomputeFromCanonical_EqualsCreateHash_ForJustWrittenFixture(bool azure)
    {
        // The whole renameable-fixture feature rests on this invariant: a fixture written
        // via Create resolves byte-identically when matched via its stored CanonicalRequest.
        var rules = new CacheKeyRules { NormalizeAzureOpenAIDeployment = azure };
        var created = RequestKeyFactory.Create(
            "POST", "/openai/deployments/prod/chat/completions", "?api-version=2024-06-01",
            Body("{\"model\":\"gpt-4\",\"messages\":[{\"role\":\"user\",\"content\":\"hello \\u00e9\"}]}"),
            new[] { "be terse" }, rules);

        var recomputed = RequestKeyFactory.ComputeKeyFromCanonical(created.Canonical, rules);

        recomputed.Should().Be(created.Hash);
    }

    [Fact]
    public async Task EnablingAzureNormalization_ReBaselinesExistingFixture_ZeroUpstream()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), $"augustus-rekey-{Guid.NewGuid():N}");
        Directory.CreateDirectory(cacheDir);
        try
        {
            // Fixture was recorded under the OLD rules (no normalization), deployment "prod".
            var legacy = RequestKeyFactory.Create(
                "POST", "/openai/deployments/prod/chat/completions", "?api-version=2024-06-01",
                Body("{\"model\":\"gpt-4\"}"), null, null);

            var fileManager = new APISimulator.FileManager(cacheDir);
            await fileManager.CacheResponseAsync(legacy.Hash, "{\"id\":\"rebaselined\"}",
                "PROXY", new List<string>(), normalized: false, legacy.Canonical);

            // Rules change: deployment-agnostic normalization is now enabled, and the
            // upstream uses a renamed deployment. No API key, no proxy — pure disk match.
            var normRules = new CacheKeyRules { NormalizeAzureOpenAIDeployment = true };
            var incoming = RequestKeyFactory.Create(
                "POST", "/openai/deployments/prod-eastus2/chat/completions", "?api-version=2024-06-01",
                Body("{\"model\":\"gpt-4\"}"), null, normRules);

            var fresh = new APISimulator.FileManager(cacheDir);
            var entry = await fresh.ResolveEntryAsync(
                incoming.Hash,
                c => RequestKeyFactory.ComputeKeyFromCanonical(c, normRules));

            entry.Should().NotBeNull();
            entry!.Response.Should().Contain("rebaselined");
        }
        finally { Directory.Delete(cacheDir, true); }
    }
}
