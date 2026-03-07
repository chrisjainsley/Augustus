using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Augustus.Extensions;
using Microsoft.Extensions.Configuration;

namespace Augustus.Sample.StripeApi.Tests;

public class StripeApiTests
{
    private static readonly IConfigurationRoot Config = new ConfigurationBuilder()
        .AddUserSecrets(Assembly.GetExecutingAssembly())
        .AddEnvironmentVariables()
        .Build();

    private static string? GetApiKey()
    {
        return Config["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    }

    private static string GetModel()
    {
        return Config["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    private static string ResolveCachePath(string apiName, [CallerFilePath] string callerFilePath = "")
    {
        var dir = Path.GetDirectoryName(callerFilePath)!;
        var className = Path.GetFileNameWithoutExtension(callerFilePath);
        return Path.Combine(dir, "__mocks__", className, apiName);
    }

    [Fact]
    public async Task CreateCharge_ShouldReturnValidJsonResponse()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            // No API key — skip. Cached responses will make this self-sufficient after first run.
            return;
        }

        await using var simulator = this.CreateStripeSimulator(opt =>
        {
            opt.OpenAIApiKey = apiKey;
            opt.OpenAIModel = GetModel();
            opt.Port = 9050;
        })
        .WithInstruction("Return realistic Stripe API JSON responses.")
        .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\", a realistic \"id\" starting with \"ch_\", the amount and currency from the request, and \"status\": \"succeeded\".");

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = "2000",
            ["currency"] = "usd",
            ["source"] = "tok_visa"
        });

        var response = await client.PostAsync("/v1/charges", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("object").GetString().Should().Be("charge");
    }

    [Fact]
    public async Task SameRequest_ShouldReturnIdenticalCachedResponse()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

        await using var simulator = this.CreateStripeSimulator(opt =>
        {
            opt.OpenAIApiKey = apiKey;
            opt.OpenAIModel = GetModel();
            opt.Port = 9051;
        })
        .WithInstruction("Return realistic Stripe API JSON responses.")
        .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\", a realistic \"id\" starting with \"ch_\", the amount and currency from the request, and \"status\": \"succeeded\".");

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        var makeRequest = async () =>
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["amount"] = "5000",
                ["currency"] = "eur",
                ["source"] = "tok_mastercard"
            });
            var resp = await client.PostAsync("/v1/charges", content);
            return await resp.Content.ReadAsStringAsync();
        };

        var response1 = await makeRequest();
        var response2 = await makeRequest();

        response1.Should().Be(response2, "cached responses should be byte-identical");

        var cachePath = ResolveCachePath("Stripe");
        Directory.Exists(cachePath).Should().BeTrue("cache folder should exist at {0}", cachePath);
        Directory.GetFiles(cachePath, "*.json").Should().NotBeEmpty("cache files should be written");
    }

    [Fact]
    public async Task RouteSpecificInstructions_ShouldReturnDifferentResponsesPerEndpoint()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

        await using var simulator = this.CreateStripeSimulator(opt =>
        {
            opt.OpenAIApiKey = apiKey;
            opt.OpenAIModel = GetModel();
            opt.Port = 9052;
        })
        .WithInstruction("Return realistic Stripe API JSON responses. Always return raw JSON only, no markdown.")
        .ForGet("/v1/customers/{id}")
            .WithInstruction("Return a customer object with \"object\": \"customer\", the \"id\" from the URL path, \"name\", and \"email\" fields.")
        .ForPost("/v1/customers")
            .WithInstruction("Return a newly created customer object with \"object\": \"customer\", an \"id\" starting with \"cus_\", and the name/email from the request body.")
        .Build();

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        var getResponse = await client.GetAsync("/v1/customers/cus_test123");
        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getDoc = JsonDocument.Parse(getJson);
        getDoc.RootElement.GetProperty("object").GetString().Should().Be("customer");

        var postContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "Jane Doe",
            ["email"] = "jane@example.com"
        });
        var postResponse = await client.PostAsync("/v1/customers", postContent);
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        postDoc.RootElement.GetProperty("id").GetString().Should().StartWith("cus_");
    }

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
