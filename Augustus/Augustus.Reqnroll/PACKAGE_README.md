# Augustus.Reqnroll

[Reqnroll](https://reqnroll.net/) (BDD/Gherkin) integration for [Augustus](https://www.nuget.org/packages/Augustus). Automatically organizes mock caches by scenario and places them next to your feature files.

[![NuGet](https://img.shields.io/nuget/v/Augustus.Reqnroll.svg)](https://www.nuget.org/packages/Augustus.Reqnroll)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/chrisjainsley/augustus/blob/master/LICENSE)

Requires the [Augustus](https://www.nuget.org/packages/Augustus) core package (installed automatically as a dependency).

## Quick Start

Register your simulator in a Reqnroll `[Binding]` hook. Augustus.Reqnroll automatically intercepts the scenario lifecycle to route cached responses into per-scenario subdirectories next to your feature files.

```csharp
using Augustus.AI;
using Augustus.Extensions;
using Augustus.Reqnroll;
using Reqnroll;

[Binding]
public class Hooks
{
    private static APISimulator? _simulator;

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        _simulator = new Hooks().CreateStripeSimulator();
        _simulator.UseAI(new AIOptions
        {
            OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
        });
        _simulator.AddInstruction("Return realistic Stripe API responses");

        AugustusReqnrollContext.Register(_simulator);
        await _simulator.StartAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        AugustusReqnrollContext.Clear();
    }
}
```

## Key Features

- **Per-scenario cache isolation** — each scenario gets its own cache directory, preventing collisions in parallel runs
- **Automatic cache placement** — mock files are stored next to your `.feature` files in a `__mocks__` directory
- **Zero configuration** — register your simulator once and the Reqnroll hooks handle the rest
- **Works with all response strategies** — static, file-based, AI-generated, or proxy responses all cache correctly

## Cache Directory Structure

Cached responses are organized automatically by API name and scenario:

```
Features/
  __mocks__/
    Stripe/
      Scenario_Name_1/
        {hash}.json
      Scenario_Name_2/
        {hash}.json
  MyFeature.feature
```

## API

```csharp
// Register a simulator (call in [BeforeTestRun])
AugustusReqnrollContext.Register(simulator);

// Retrieve a registered simulator by index
var sim = AugustusReqnrollContext.GetRegisteredSimulator(0);

// Clean up all registered simulators (call in [AfterTestRun])
AugustusReqnrollContext.Clear();
```

## Related Packages

| Package | Purpose |
|---------|---------|
| [Augustus](https://www.nuget.org/packages/Augustus) | Core simulator — route matching, static responses, caching |
| [Augustus.AI](https://www.nuget.org/packages/Augustus.AI) | AI-powered response generation and real-API proxy via OpenAI |
| [Augustus.Stripe](https://www.nuget.org/packages/Augustus.Stripe) | Pre-built Stripe API defaults and fluent helpers |
| [Augustus.GitHub](https://www.nuget.org/packages/Augustus.GitHub) | Pre-built GitHub API defaults and fluent helpers |

## Documentation

Full documentation and examples: [github.com/chrisjainsley/augustus](https://github.com/chrisjainsley/augustus)
