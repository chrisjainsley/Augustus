using System.Text;
using System.Text.Json;
using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class CustomerTests
{
    [Fact]
    public async Task RouteSpecificInstructions_ShouldReturnDifferentResponsesPerEndpoint()
    {
        if (!IsStripeSampleCiCacheOnly() && string.IsNullOrEmpty(GetApiKey()))
            return;

        await using var simulator = this.CreateStripeSimulator(opt => ApplyStripeSimulatorTransport(opt, localPort: 9052))
            .WithInstruction("Return realistic Stripe API JSON responses. Always return raw JSON only, no markdown.")
            .ForGet("/v1/customers/{id}")
            .WithInstruction("Return a customer object with \"object\": \"customer\", the \"id\" from the URL path, \"name\", and \"email\" fields.")
            .ForPost("/v1/customers")
            .WithInstruction("Return a newly created customer object with \"object\": \"customer\", an \"id\" starting with \"cus_\", and the name/email from the request body.")
            .Build();

        UseStripeAi(simulator);

        await simulator.StartAsync();
        var client = simulator.CreateClient();

        var getResponse = await client.GetAsync("/v1/customers/cus_test123");
        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getDoc = JsonDocument.Parse(getJson);
        getDoc.RootElement.GetProperty("object").GetString().Should().Be("customer");

        if (IsStripeSampleCiCacheOnly())
            AssertCommittedMockExists("1F38BC7FF6786FE4B58C15243A02C882A1DA3F75D2934E1894DF4EFB3957B1A5");

        // JSON POST exercises canonical JSON body hashing for cache keys (cross-OS).
        var postBody = """{"name":"Jane Doe","email":"jane@example.com"}""";
        using var postRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/customers")
        {
            Content = new StringContent(postBody, Encoding.UTF8, "application/json")
        };

        var postResponse = await client.SendAsync(postRequest);
        postResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postDoc = JsonDocument.Parse(postJson);
        postDoc.RootElement.GetProperty("id").GetString().Should().StartWith("cus_");

        if (IsStripeSampleCiCacheOnly())
            AssertCommittedMockExists("0FACAE8B0803194922EB8602D1D467706FEC0390AB61A72DEECBA91BD8F0BC0E");
    }
}
