# Augustus.AI

OpenAI-powered extension for [Augustus](https://www.nuget.org/packages/Augustus). Generates realistic API responses using AI and supports proxying to real APIs with caching.

[![NuGet](https://img.shields.io/nuget/v/Augustus.AI.svg)](https://www.nuget.org/packages/Augustus.AI)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/chrisjainsley/augustus/blob/master/LICENSE)

Requires the [Augustus](https://www.nuget.org/packages/Augustus) core package (installed automatically as a dependency).

## Quick Start

### AI default handler for unmatched routes

```csharp
using Augustus.AI;
using Augustus.Extensions;

var simulator = this.CreateStripeSimulator(opt => opt.Port = 0);
simulator.UseAI(new AIOptions
{
    OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
});

// Static override — always served from this JSON
simulator.ForGet("/v1/customers/cus_known")
    .WithResponse(new { id = "cus_known", name = "John Doe" })
    .Add();

// Everything else handled by AI
simulator.AddInstruction("Return realistic Stripe API responses");

await simulator.StartAsync();
var client = simulator.CreateClient();

// Hits the static route
var known = await client.GetStringAsync("/v1/customers/cus_known");

// Falls through to AI
var generated = await client.GetStringAsync("/v1/customers/cus_other");

await simulator.StopAsync();
```

### Per-route AI

```csharp
simulator.ForGet("/v1/payments/{id}")
    .UseAI(aiOptions, "Return a completed payment object")
    .Add();

simulator.ForPost("/v1/payments")
    .WithResponse(new { id = "pay_static", status = "pending" })
    .Add();
```

### Real API proxy

Forward requests to an upstream API, cache responses, and replay on subsequent calls:

```csharp
var simulator = this.CreateAPISimulator("OpenAI Proxy");
simulator.UseProxy(
    new AIOptions
    {
        OpenAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
    },
    upstreamUrl: "https://api.openai.com"
);
await simulator.StartAsync();

var client = simulator.CreateClient();
// First call: forwarded to real API, response cached
// Second call: served from cache instantly
var response = await client.PostAsync("/v1/chat/completions", content);
```

## Key Features

- **AI response generation** — generate realistic API responses via OpenAI for any unmatched route
- **Per-route AI** — use `UseAI()` on individual routes with custom instructions
- **Real API proxy** — forward to upstream APIs and cache responses for replay
- **Azure OpenAI support** — use Azure-hosted models with `UseAzureOpenAI`
- **Response caching** — AI and proxy responses cached to disk for fast, deterministic reruns
- **Rate limit handling** — shared throttling, in-flight deduplication, exponential backoff with jitter
- **Cache-only mode** — run in CI without an API key using pre-recorded responses

## Configuration

```csharp
simulator.UseAI(new AIOptions
{
    OpenAIApiKey = "sk-...",              // Required (unless CacheOnly)
    OpenAIModel = "gpt-4o-mini",         // Default model
    OpenAIEndpoint = "",                  // Custom endpoint (optional)
    UseAzureOpenAI = false,               // Use Azure OpenAI service
    AzureDeploymentName = "",             // Required when UseAzureOpenAI = true
    AzureApiVersion = "2024-06-01",       // Azure API version
    MaxRetries = 5,                       // Retry attempts for transient failures
    MaxConcurrentRequests = 10            // Process-wide concurrent OpenAI limit
});
```

## Rate Limits and Efficiency

- **Shared throttling** — all OpenAI calls in the process share one concurrency gate per API key/model combination
- **In-flight deduplication** — concurrent requests with the same cache key share a single OpenAI completion
- **Retries** — transient failures (429, 5xx) use exponential backoff with jitter, respecting `Retry-After` headers
- **Tip** — for first-time cache generation, use low `MaxConcurrentRequests` (1-2) and run tests serially to minimize billed retries

## Related Packages

| Package | Purpose |
|---------|---------|
| [Augustus](https://www.nuget.org/packages/Augustus) | Core simulator — route matching, static responses, caching |
| [Augustus.Stripe](https://www.nuget.org/packages/Augustus.Stripe) | Pre-built Stripe API defaults and fluent helpers |
| [Augustus.GitHub](https://www.nuget.org/packages/Augustus.GitHub) | Pre-built GitHub API defaults and fluent helpers |
| [Augustus.Reqnroll](https://www.nuget.org/packages/Augustus.Reqnroll) | Reqnroll (BDD) integration with per-scenario cache isolation |

## Documentation

Full documentation and examples: [github.com/chrisjainsley/augustus](https://github.com/chrisjainsley/augustus)
