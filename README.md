# Augustus

A modular API simulator library for .NET. Serve static JSON, load from files, or generate realistic responses with AI — all from a lightweight local web server in your tests.

[![NuGet Augustus](https://img.shields.io/nuget/v/Augustus.svg?label=Augustus)](https://www.nuget.org/packages/Augustus)
[![NuGet Augustus.AI](https://img.shields.io/nuget/v/Augustus.AI.svg?label=Augustus.AI)](https://www.nuget.org/packages/Augustus.AI)
[![NuGet Augustus.AI.Reqnroll](https://img.shields.io/nuget/v/Augustus.AI.Reqnroll.svg?label=Augustus.AI.Reqnroll)](https://www.nuget.org/packages/Augustus.AI.Reqnroll)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

## Packages

| Package | Purpose | AI Required? |
|---------|---------|:------------:|
| **Augustus** | Core simulator — static JSON, file-based, and custom response strategies with route matching | No |
| **Augustus.AI** | AI-powered response generation and real-API proxy mode via OpenAI | Yes |
| **Augustus.APIs.Stripe** | Pre-built Stripe API defaults and fluent helpers | No |
| **Augustus.Reqnroll** | Reqnroll (BDD) integration with per-scenario cache isolation | No |

## Quick Start

### Manual mocks only (no AI, no API key)

```bash
dotnet add package Augustus
```

```csharp
using Augustus.Extensions;

public class StripeTests
{
    [Fact]
    public async Task Should_Return_Customer_Data()
    {
        var simulator = this.CreateAPISimulator("Stripe")
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
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("cus_123");

        await simulator.StopAsync();
    }
}
```

### AI default handler for unmatched routes

```bash
dotnet add package Augustus.AI
```

```csharp
using Augustus.AI;
using Augustus.Extensions;

public class StripeTests
{
    [Fact]
    public async Task Should_Return_Customer_Data()
    {
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
        known.Should().Contain("John Doe");

        // Falls through to AI
        var generated = await client.GetStringAsync("/v1/customers/cus_other");
        generated.Should().Contain("cus_other");

        await simulator.StopAsync();
    }
}
```

### Per-route AI

```csharp
using Augustus.AI;

simulator.ForGet("/v1/payments/{id}")
    .UseAI(aiOptions, "Return a completed payment object")
    .Add();

simulator.ForPost("/v1/payments")
    .WithResponse(new { id = "pay_static", status = "pending" })
    .Add();
```

## Route Resolution

Requests are resolved in this order:

1. **Route with strategy** — execute the matched `IResponseStrategy` (static, file, AI, or custom)
2. **Default handler** — delegate to the AI or proxy handler (if installed via `UseAI` / `UseProxy`)
3. **No match, no default handler** — HTTP 404 JSON error

## Configuration

### Core options (`APISimulatorOptions`)

```csharp
var simulator = this.CreateAPISimulator("MyAPI", options =>
{
    options.Port = 0;                        // 0 = auto-assign (default: 9001)
    options.EnableCaching = true;            // Cache AI/proxy responses (default: true)
    options.CacheFolderPath = "./mocks";     // Where cache files live (default: ./mocks)
    options.CacheOnly = false;               // Serve only from cache, 503 on miss (default: false)
    options.AutoRemoveStaleCache = true;     // Clean up untouched cache files on dispose (default: true)

    // Stable, renameable, hand-authorable fixtures (see "Cache identity" below)
    options.NormalizeAzureOpenAIDeployment = true;            // /openai/deployments/{x}/ → constant
    options.RequestKeyTransform = key => key with { ... };    // strip/rewrite volatile request parts
    options.IgnoredQueryParameters.Add("_cb");                // drop cache-buster params from the key
    options.StripNullBodyProperties = true;                   // null vs omitted no longer churns
    options.HashMessagesContentOnly = true;                   // only messages[].content keys the cache
    options.OnCacheMiss = d =>                                // discover the expected identity
        Console.WriteLine($"miss: {d.ComputedKey} {d.ExpectedCanonicalRequest.Path}");
});
```

### AI options (`AIOptions` — Augustus.AI package)

```csharp
simulator.UseAI(new AIOptions
{
    OpenAIApiKey = "sk-...",                 // Required (unless CacheOnly)
    OpenAIModel = "gpt-4o-mini",            // Default: gpt-4o-mini
    OpenAIEndpoint = "",                     // Optional: custom endpoint
    UseAzureOpenAI = false,                  // Use Azure OpenAI service
    AzureDeploymentName = "",                // Required when UseAzureOpenAI = true
    AzureApiVersion = "2024-06-01",          // Azure API version
    MaxRetries = 5,                          // Retry attempts for transient failures
    MaxConcurrentRequests = 10               // Process-wide concurrent OpenAI limit (see below)
});
```

### OpenAI rate limits and call efficiency

Augustus.AI reduces duplicate traffic and `429` rate-limit pressure in several ways:

- **Shared throttling** — For a given API key (or Azure endpoint + key), model/deployment, and `MaxConcurrentRequests` value, all OpenAI chat completions in the process share one concurrency gate. This applies across the default `UseAI` handler and route-level `UseAI`, so parallel test classes do not each get a full `MaxConcurrentRequests` budget in isolation.
- **In-flight deduplication** — Concurrent requests that resolve to the same cache key (same method, path, query, normalized body, and route instructions) share a single in-flight OpenAI completion instead of fanning out.
- **Retries** — Transient failures (`429`, `5xx`) use exponential backoff with **jitter**. When the service returns a `Retry-After` header, the delay respects it (in addition to backoff caps from `InitialRetryDelayMs` / `MaxRetryDelayMs`).
- **Proxy / `UseRealApi`** — Upstream HTTP calls retry on `429` and `5xx` with the same delay settings; global `DynamicContentFields` from `APISimulatorOptions` are merged with per-route `WithDynamicFields` for cache keys.
- **Practical tips** — For first-time cache generation, prefer a **low** `MaxConcurrentRequests` (for example `1`–`2`) and/or run tests **serially** so retries do not multiply billed attempts. Use **cache-only** CI (`CacheOnly` / committed mocks) so CI never calls OpenAI.

## Response Strategies

### Static JSON

```csharp
simulator.ForGet("/api/health")
    .WithResponse(new { status = "ok" })
    .WithStatusCode(200)
    .Add();
```

### JSON file

```csharp
simulator.ForGet("/v1/customers/{id}")
    .WithJsonFile("./mocks/customer.json")
    .Add();
```

### Custom strategy

```csharp
simulator.ForPost("/api/echo")
    .WithStrategy(new MyCustomStrategy())
    .Add();
```

### AI-generated (Augustus.AI)

Route-level `UseAI` uses the same **structured cache keys** as the default AI handler (`CacheKeyComputer`), stores entries in the simulator’s cache folder, and still reads **legacy** curl-based cache files if present. Azure OpenAI is supported on routes the same way as `simulator.UseAI(...)`.

```csharp
simulator.ForGet("/v1/payments/{id}")
    .UseAI(aiOptions, "Return a completed payment with realistic fields")
    .Add();
```

### Real API proxy (Augustus.AI)

```csharp
simulator.ForPost("/v1/chat/completions")
    .UseRealApi("https://api.openai.com", apiKey, aiOptions)
    .Add();
```

## Extension Methods

```csharp
// Core (Augustus package) — no AI dependency
var sim = this.CreateAPISimulator("MyAPI");
var stripe = this.CreateStripeSimulator();
var paypal = this.CreatePayPalSimulator();

// AI-powered (Augustus.AI package)
var openai = this.CreateOpenAISimulator(opt => { opt.OpenAIApiKey = key; });
var azure = this.CreateAzureOpenAISimulator(opt => { ... });

// Proxies (Augustus.AI package)
var proxy = this.CreateOpenAIProxy(opt => { opt.OpenAIApiKey = key; });
var azureProxy = this.CreateAzureOpenAIProxy(opt => { ... });
```

## Pass-Through Proxy Mode

Forward requests to a real upstream API, cache responses, and replay on subsequent calls. Ideal for tool/function calling scenarios.

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

## Cache-Only Mode

Replay cached responses without an API key or network — perfect for CI/CD.

```csharp
var simulator = this.CreateAPISimulator("Stripe", options =>
{
    options.CacheOnly = true;
    options.CacheFolderPath = "./pre-recorded-mocks";
});
await simulator.StartAsync();
// Returns cached responses or HTTP 503 on cache miss
```

## Reqnroll (BDD) Integration

```bash
dotnet add package Augustus.Reqnroll
```

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
}
```

Cached responses are organized per scenario:
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

## Caching

Cache keys are derived from the HTTP method, path, query string, and a **canonical form of JSON request bodies** (sorted object keys at every depth, plus optional `DynamicContentFields` normalization). Non-JSON bodies use raw bytes. See [`CHANGELOG.md`](CHANGELOG.md) for release notes when this algorithm changes — **upgrades can invalidate existing on-disk caches**.

### Cache identity (renameable / hand-authorable fixtures)

Each fixture stores its `CanonicalRequest`, and matching is **content-based**: the file name is a free-form label, not the key. So you can:

- **Rename or hand-author** a fixture file to any name — it still resolves.
- Keep keys stable across incidental request changes with `NormalizeAzureOpenAIDeployment`, `RequestKeyTransform`, `IgnoredQueryParameters`, `StripNullBodyProperties`, or `HashMessagesContentOnly`.
- Use `OnCacheMiss` (or the diagnostic fields in the `503` body) to discover the exact identity Augustus expected, so a fixture can be authored offline.
- Re-baseline committed fixtures after a keying-rule change **with zero upstream calls** via `CacheMaintenance.Rekey(path, new RekeyOptions { … })`.

Legacy fixtures without a `CanonicalRequest` keep resolving by their original hash file name, and the default key is unchanged — no rekey is needed unless you opt into a new rule.

Augustus caches responses as JSON files:

```json
{
  "RequestHash": "A1B2C3D4E5F6G7H8",
  "Response": "{\"id\": \"cus_123\", \"name\": \"John Doe\"}",
  "OriginalRequest": "curl -X GET http://localhost:9001/v1/customers/cus_123",
  "Instructions": ["You are a Stripe API simulator"],
  "Timestamp": "2024-01-15T10:30:00Z"
}
```

```csharp
simulator.ClearCache();                    // Clear all cached responses
simulator.SetTestContext("my-scenario");   // Route cache to a subdirectory
simulator.ClearTestContext();              // Clear subdirectory and remove stale entries
```

## Package Dependency Graph

```
Augustus (core) — no OpenAI dependency
  └── Microsoft.AspNetCore.App (framework ref only)

Augustus.AI → Augustus
  ├── OpenAI (2.9.1)
  └── Azure.AI.OpenAI (2.1.0)

Augustus.APIs.Stripe → Augustus

Augustus.Reqnroll → Augustus
  └── Reqnroll packages
```

## Best Practices

1. **Start with static mocks** — use `WithResponse` / `WithJsonFile` for deterministic tests; add AI only where needed
2. **Use `Port = 0`** — auto-assign ports to avoid conflicts in parallel test runs
3. **Enable caching** — dramatically speeds up AI-powered tests and reduces API costs
4. **Use cache-only in CI** — commit cached responses and run without an API key
5. **Use `await using`** — ensures proper cleanup of the simulator's web server
6. **Proxy mode for tool calling** — when testing function/tool calling, use proxy mode to get real model reasoning

## Building from Source

```bash
git clone https://github.com/chrisjainsley/augustus.git
cd augustus
dotnet build
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

If you encounter any issues or have questions:

1. Search existing [issues](https://github.com/chrisjainsley/augustus/issues)
2. Create a new issue if needed

## Roadmap

- [x] Route-based response dispatch with pluggable strategies
- [x] AI-powered response generation via OpenAI (Augustus.AI)
- [x] Pass-through proxy mode with response caching
- [x] Cache-only mode for CI/offline testing
- [x] BDD integration (Reqnroll)
- [x] Dynamic port assignment for parallel testing
- [ ] More API-specific packages (PayPal, Twilio, etc.)
- [ ] Support for more AI providers (Anthropic Claude, Google Gemini)
- [ ] Request/response validation
- [ ] Docker support
