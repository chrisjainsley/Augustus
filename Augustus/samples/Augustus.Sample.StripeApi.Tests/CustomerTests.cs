using System.Text.Json;
using Augustus.AI;
using Augustus.Extensions;
using static Augustus.Sample.StripeApi.Tests.TestConfiguration;

namespace Augustus.Sample.StripeApi.Tests;

public class CustomerTests
{
    [Fact]
    public async Task RouteSpecificInstructions_ShouldReturnDifferentResponsesPerEndpoint()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            return;

        await using var simulator = this.CreateStripeSimulator(opt =>
        {
            opt.Port = 9052;
        })
        .WithInstruction("Return realistic Stripe API JSON responses. Always return raw JSON only, no markdown.")
        .ForGet("/v1/customers/{id}")
            .WithInstruction("Return a customer object with \"object\": \"customer\", the \"id\" from the URL path, \"name\", and \"email\" fields.")
        .ForPost("/v1/customers")
            .WithInstruction("Return a newly created customer object with \"object\": \"customer\", an \"id\" starting with \"cus_\", and the name/email from the request body.")
        .Build();

        simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = apiKey,
            OpenAIModel = GetModel()
        });

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
}
