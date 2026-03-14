using Augustus.Extensions;
using Augustus.Reqnroll;
using Reqnroll;

namespace Augustus.Sample.Reqnroll.Specs.Support;

[Binding]
public class Hooks
{
    private static APISimulator? _simulator;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        var apiKey = TestConfiguration.GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException(
                "OPENAI_API_KEY is not configured. Set it via user-secrets or environment variable to run these sample specs.");

        _simulator = new Hooks().CreateStripeSimulator(opt =>
        {
            opt.OpenAIApiKey = apiKey;
            opt.OpenAIModel = TestConfiguration.GetModel();
            opt.Port = 9060;
        })
        .WithInstruction("Return realistic Stripe API JSON responses.")
        .WithInstruction("For POST /v1/charges, return a charge object with \"object\": \"charge\", a realistic \"id\" starting with \"ch_\", the amount and currency from the request, and \"status\": \"succeeded\".");

        AugustusReqnrollContext.Register(_simulator);

        await _simulator.StartAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        AugustusReqnrollContext.Clear();
        if (_simulator != null)
            await _simulator.DisposeAsync();
    }
}
