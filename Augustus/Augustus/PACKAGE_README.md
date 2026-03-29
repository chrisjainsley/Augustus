# Augustus

A lightweight API simulator for .NET. Serve static JSON, load from files, or plug in custom response strategies — all from a local web server running inside your tests.

[![NuGet](https://img.shields.io/nuget/v/Augustus.svg)](https://www.nuget.org/packages/Augustus)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/chrisjainsley/augustus/blob/master/LICENSE)

## Quick Start

```csharp
using Augustus.Extensions;

public class ApiTests
{
    [Fact]
    public async Task Should_Return_Customer_Data()
    {
        var simulator = this.CreateAPISimulator("MyAPI")
            .ForGet("/v1/customers/{id}")
                .WithJsonFile("./mocks/customer.json")
                .Add()
            .ForPost("/v1/charges")
                .WithResponse(new { id = "ch_123", amount = 2000, currency = "usd", status = "succeeded" })
                .Add();

        await simulator.StartAsync();

        var client = simulator.CreateClient();
        var response = await client.GetAsync("/v1/customers/cus_123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await simulator.StopAsync();
    }
}
```

## Key Features

- **Route matching** with path parameters (`/v1/customers/{id}`) and wildcard support
- **Static JSON responses** — inline objects or string literals
- **File-based responses** — serve JSON from disk with `WithJsonFile()`
- **Custom strategies** — implement `IResponseStrategy` for full control
- **Request verification** — assert requests were received with `Verify()`
- **Webhook delivery** — simulate outbound webhook events triggered by incoming requests
- **Response caching** — cache and replay responses for fast, deterministic tests
- **Cache-only mode** — run in CI without network access using pre-recorded mocks
- **Auto port assignment** — use `Port = 0` to avoid conflicts in parallel test runs
- **Framework agnostic** — works with xUnit, NUnit, MSTest, or any test runner

## Response Strategies

```csharp
// Static JSON
simulator.ForGet("/api/health")
    .WithResponse(new { status = "ok" })
    .WithStatusCode(200)
    .Add();

// JSON file
simulator.ForGet("/v1/customers/{id}")
    .WithJsonFile("./mocks/customer.json")
    .Add();

// Custom strategy
simulator.ForPost("/api/echo")
    .WithStrategy(new MyCustomStrategy())
    .Add();
```

## Request Verification

```csharp
await simulator.StartAsync();
var client = simulator.CreateClient();
await client.PostAsync("/v1/charges", content);

// Assert the request was received
simulator.Verify(r => r.Path == "/v1/charges" && r.Method == HttpMethod.Post)
    .WasCalledOnce();

simulator.Verify(r => r.Path == "/v1/refunds")
    .WasNeverCalled();
```

## Webhook Delivery

```csharp
var simulator = this.CreateAPISimulator("Stripe")
    .WithWebhook("https://myapp.test/webhooks/stripe")
    .OnRequest(HttpMethod.Post, "/v1/charges")
        .FireWebhookEvent("charge.created")
        .WithPayload(new { type = "charge.created", data = new { id = "ch_123" } })
        .WithDelay(TimeSpan.FromMilliseconds(100))
        .Add();
```

## Configuration

```csharp
var simulator = this.CreateAPISimulator("MyAPI", options =>
{
    options.Port = 0;                        // 0 = auto-assign (default: 9001)
    options.EnableCaching = true;            // Cache responses (default: true)
    options.CacheFolderPath = "./mocks";     // Cache location (default: ./mocks)
    options.CacheOnly = false;               // Serve only from cache (default: false)
    options.AutoRemoveStaleCache = true;     // Clean up unused cache files (default: true)
});
```

## Route Resolution Order

1. **Route with strategy** — matched route executes its configured `IResponseStrategy`
2. **Default handler** — falls through to AI or proxy handler (if installed via Augustus.AI)
3. **No match** — returns HTTP 404 JSON error

## Related Packages

| Package | Purpose |
|---------|---------|
| [Augustus.AI](https://www.nuget.org/packages/Augustus.AI) | AI-powered response generation and real-API proxy via OpenAI |
| [Augustus.Stripe](https://www.nuget.org/packages/Augustus.Stripe) | Pre-built Stripe API defaults and fluent helpers |
| [Augustus.GitHub](https://www.nuget.org/packages/Augustus.GitHub) | Pre-built GitHub API defaults and fluent helpers |
| [Augustus.Reqnroll](https://www.nuget.org/packages/Augustus.Reqnroll) | Reqnroll (BDD) integration with per-scenario cache isolation |

## Documentation

Full documentation and examples: [github.com/chrisjainsley/augustus](https://github.com/chrisjainsley/augustus)
