using System.Text.Json;
using FluentAssertions;
using Reqnroll;

namespace Augustus.Sample.Reqnroll.Specs.Features.Charges;

[Binding]
public class CreateChargeSteps
{
    private readonly ScenarioContext _ctx;
    private HttpResponseMessage? _response;

    public CreateChargeSteps(ScenarioContext ctx) => _ctx = ctx;

    private APISimulator Simulator => _ctx.Get<APISimulator>(nameof(APISimulator));

    [Given("the Stripe API simulator is running")]
    public void GivenSimulatorRunning() { /* started in BeforeTestRun */ }

    [When("I create a charge for {int} {word} with source {string}")]
    public async Task WhenCreateCharge(int amount, string currency, string source)
    {
        var client = Simulator.CreateClient();
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["amount"] = amount.ToString(),
            ["currency"] = currency.ToLower(),
            ["source"] = source
        });
        _response = await client.PostAsync("/v1/charges", content);
    }

    [Then("the response should be successful")]
    public void ThenResponseSuccessful() =>
        _response!.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

    [Then("the response should contain a charge object")]
    public async Task ThenContainsCharge()
    {
        var json = await _response!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("object").GetString().Should().Be("charge");
    }

    [Then("the charge currency should be {string}")]
    public async Task ThenChargeCurrency(string currency)
    {
        var json = await _response!.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("currency").GetString().Should().Be(currency);
    }
}
