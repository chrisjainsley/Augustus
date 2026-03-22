using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class CacheManagementTests
{
    [Fact]
    public async Task StaleCache_ShouldBeRemovedOnDispose()
    {
        if (IsStripeSampleCiCacheOnly())
        {
            await AssertCacheOnlyChargeHitAsync(
                localPort: 9053,
                amount: "1000",
                currency: "usd",
                source: "tok_visa",
                committedHash: "6170F0873A1DF314EB4583DDFA2541D1549CBF24DCAF9C4888A0E7F2400AF91A");
            return;
        }

        if (string.IsNullOrEmpty(GetApiKey()))
            return;

        var cachePath = ResolveCachePath("Stripe");

        string stalePath;

        {
            await using var simulator = this.CreateStripeSimulator(opt =>
            {
                opt.Port = 9053;
                opt.AutoRemoveStaleCache = true;
            })
            .WithInstruction("Return realistic Stripe API JSON responses.")
            .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\".");

            UseStripeAi(simulator);

            await simulator.StartAsync();
            var client = simulator.CreateClient();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["amount"] = "1000",
                ["currency"] = "usd",
                ["source"] = "tok_visa"
            });
            await client.PostAsync("/v1/charges", content);

            // Plant a fake stale cache file
            stalePath = Path.Combine(cachePath, "STALE_HASH.json");
            await File.WriteAllTextAsync(stalePath, "{\"Response\":\"stale\"}");

            File.Exists(stalePath).Should().BeTrue("stale file should exist before dispose");
        }
        // Simulator is disposed here via await using

        File.Exists(stalePath).Should().BeFalse("stale cache file should be removed on dispose");

        // Real cache files should survive
        Directory.GetFiles(cachePath, "*.json").Should().NotBeEmpty("real cache files should survive dispose");
    }

    [Fact]
    public async Task AutoRemoveStaleCache_WhenDisabled_ShouldKeepStaleFiles()
    {
        if (IsStripeSampleCiCacheOnly())
        {
            await AssertCacheOnlyChargeHitAsync(
                localPort: 9054,
                amount: "3000",
                currency: "gbp",
                source: "tok_visa",
                committedHash: "4995720C5B6986DE418E4F17DE3689CEB561326199A57DBC72443CCE6CC83993");
            return;
        }

        if (string.IsNullOrEmpty(GetApiKey()))
            return;

        var cachePath = ResolveCachePath("Stripe");

        string stalePath;

        {
            await using var simulator = this.CreateStripeSimulator(opt =>
            {
                opt.Port = 9054;
                opt.AutoRemoveStaleCache = false;
            })
            .WithInstruction("Return realistic Stripe API JSON responses.")
            .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\".");

            UseStripeAi(simulator);

            await simulator.StartAsync();
            var client = simulator.CreateClient();

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["amount"] = "3000",
                ["currency"] = "gbp",
                ["source"] = "tok_visa"
            });
            await client.PostAsync("/v1/charges", content);

            // Plant a fake stale cache file
            stalePath = Path.Combine(cachePath, "STALE_HASH.json");
            await File.WriteAllTextAsync(stalePath, "{\"Response\":\"stale\"}");
        }
        // Simulator is disposed here via await using

        File.Exists(stalePath).Should().BeTrue("stale cache file should survive when AutoRemoveStaleCache is disabled");

        // Cleanup
        try { File.Delete(stalePath); } catch { /* ignore */ }
    }

    private async Task AssertCacheOnlyChargeHitAsync(
        int localPort,
        string amount,
        string currency,
        string source,
        string committedHash)
    {
        await using var simulator = this.CreateStripeSimulator(opt =>
        {
            ApplyStripeSimulatorTransport(opt, localPort);
            if (IsStripeSampleCiCacheOnly())
            {
                // CacheOnly forces AutoRemoveStaleCache=false; keep explicit for clarity.
                opt.AutoRemoveStaleCache = false;
            }
        })
        .WithInstruction("Return realistic Stripe API JSON responses.")
        .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\".");

        UseStripeAi(simulator);

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = amount,
            ["currency"] = currency,
            ["source"] = source
        });
        var response = await client.PostAsync("/v1/charges", content);
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        AssertCommittedMockExists(committedHash);
    }
}
