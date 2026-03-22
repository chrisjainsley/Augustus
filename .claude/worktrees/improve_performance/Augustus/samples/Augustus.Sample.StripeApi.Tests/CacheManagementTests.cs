using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class CacheManagementTests
{
    [Fact]
    public async Task StaleCache_ShouldBeRemovedOnDispose()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

        var cachePath = ResolveCachePath("Stripe");

        string stalePath;

        {
            await using var simulator = this.CreateStripeSimulator(opt =>
            {
                opt.OpenAIApiKey = apiKey;
                opt.OpenAIModel = GetModel();
                opt.Port = 9053;
                opt.AutoRemoveStaleCache = true;
            })
            .WithInstruction("Return realistic Stripe API JSON responses.")
            .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\".");

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
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

        var cachePath = ResolveCachePath("Stripe");

        string stalePath;

        {
            await using var simulator = this.CreateStripeSimulator(opt =>
            {
                opt.OpenAIApiKey = apiKey;
                opt.OpenAIModel = GetModel();
                opt.Port = 9054;
                opt.AutoRemoveStaleCache = false;
            })
            .WithInstruction("Return realistic Stripe API JSON responses.")
            .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\".");

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
        try { File.Delete(stalePath); } catch { }
    }
}
