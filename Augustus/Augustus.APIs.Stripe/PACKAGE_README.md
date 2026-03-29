# Augustus.Stripe

Pre-built Stripe API defaults and fluent helpers for [Augustus](https://www.nuget.org/packages/Augustus). Mock Stripe endpoints with realistic responses in a few lines of code.

[![NuGet](https://img.shields.io/nuget/v/Augustus.Stripe.svg)](https://www.nuget.org/packages/Augustus.Stripe)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/chrisjainsley/augustus/blob/master/LICENSE)

Requires the [Augustus](https://www.nuget.org/packages/Augustus) core package (installed automatically as a dependency).

## Quick Start

### Use all defaults

Register every Stripe endpoint with realistic default responses in one call:

```csharp
using Augustus.APIs.Stripe;

var mock = this.CreateStripeMock(opt => opt.Port = 0);
mock.UseAllDefaults();

await mock.StartAsync();

var client = mock.CreateClient();
var response = await client.GetAsync("/v1/customers/cus_123");
// Returns a realistic Stripe customer object

await mock.StopAsync();
```

### Configure specific endpoints

```csharp
var mock = this.CreateStripeMock(opt => opt.Port = 0);

// Use built-in defaults for specific resources
mock.Customers().Get().UseDefault();
mock.Customers().List().UseDefault();
mock.PaymentIntents().Create().UseDefault();

// Override with custom responses
mock.Charges().Get().WithResponse(new
{
    id = "ch_custom",
    amount = 5000,
    currency = "usd",
    status = "succeeded"
});

await mock.StartAsync();
```

### Use from a generic simulator

```csharp
using Augustus.Extensions;
using Augustus.APIs.Stripe;

var simulator = this.CreateAPISimulator("Stripe", opt => opt.Port = 0);
simulator.Stripe().Customers().Get().UseDefault();
simulator.Stripe().PaymentIntents().Create().UseDefault();

await simulator.StartAsync();
```

## Key Features

- **One-line setup** — `UseAllDefaults()` registers all Stripe endpoints with realistic responses
- **Fluent resource builders** — type-safe API for configuring individual Stripe resources
- **Realistic defaults** — built-in responses that match Stripe's actual API shape
- **Custom overrides** — replace any default with inline JSON, objects, or file-based responses
- **Error simulation** — return pre-configured Stripe error responses
- **Webhook events** — simulate Stripe webhook delivery with signing secrets
- **Latency simulation** — add realistic latency with `WithLatency(meanMs, stdDevMs)`

## Supported Resources

| Builder | Endpoints |
|---------|-----------|
| `Customers()` | Get, List, Create, Update, Delete |
| `Charges()` | Get, List, Create, Capture |
| `PaymentIntents()` | Get, List, Create, Confirm, Cancel |
| `PaymentMethods()` | Get, List, Create, Update, Attach, Detach |
| `Subscriptions()` | Get, List, Create, Update, Cancel |
| `Invoices()` | Get, List, Create, Update, Delete, Finalize, Pay, Void |
| `InvoiceItems()` | Get, List, Create, Update, Delete |
| `Refunds()` | Get, List, Create, Update |
| `Disputes()` | Get, List |
| `Products()` | Get, List, Create |
| `Prices()` | Get, List, Create |
| `Coupons()` | Get, List, Create, Delete |
| `Payouts()` | Get, List, Create |
| `Balance()` | Get |
| `BalanceTransactions()` | Get, List |
| `Events()` | Get, List |
| `Tokens()` | Create |
| `SetupIntents()` | Get, List, Create, Confirm, Cancel |

## Webhook Simulation

```csharp
var mock = this.CreateStripeMock(opt => opt.Port = 0);
mock.UseAllDefaults();
mock.WithWebhook("https://myapp.test/webhooks/stripe", signingSecret: "whsec_test123")
    .OnRequest(HttpMethod.Post, "/v1/charges")
        .FireWebhookEvent("charge.created")
        .WithPayload(new { type = "charge.created", data = new { id = "ch_123" } })
        .Add();
```

## Related Packages

| Package | Purpose |
|---------|---------|
| [Augustus](https://www.nuget.org/packages/Augustus) | Core simulator — route matching, static responses, caching |
| [Augustus.AI](https://www.nuget.org/packages/Augustus.AI) | AI-powered response generation and real-API proxy via OpenAI |
| [Augustus.GitHub](https://www.nuget.org/packages/Augustus.GitHub) | Pre-built GitHub API defaults and fluent helpers |
| [Augustus.Reqnroll](https://www.nuget.org/packages/Augustus.Reqnroll) | Reqnroll (BDD) integration with per-scenario cache isolation |

## Documentation

Full documentation and examples: [github.com/chrisjainsley/augustus](https://github.com/chrisjainsley/augustus)
