using System.Text.Json;
using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class ChargeTests
{
    [Fact]
    public async Task CreateCharge_ShouldReturnValidJsonResponse()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

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
}
