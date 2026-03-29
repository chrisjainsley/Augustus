# Augustus.GitHub

Pre-built GitHub REST API defaults and fluent helpers for [Augustus](https://www.nuget.org/packages/Augustus). Mock GitHub endpoints with realistic responses in a few lines of code.

[![NuGet](https://img.shields.io/nuget/v/Augustus.GitHub.svg)](https://www.nuget.org/packages/Augustus.GitHub)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](https://github.com/chrisjainsley/augustus/blob/master/LICENSE)

Requires the [Augustus](https://www.nuget.org/packages/Augustus) core package (installed automatically as a dependency).

## Quick Start

### Use all defaults

Register every GitHub endpoint with realistic default responses in one call:

```csharp
using Augustus.APIs.GitHub;

var mock = this.CreateGitHubMock(opt => opt.Port = 0);
mock.UseAllDefaults();

await mock.StartAsync();

var client = mock.CreateClient();
var response = await client.GetAsync("/repos/octocat/hello-world");
// Returns a realistic GitHub repository object

await mock.StopAsync();
```

### Configure specific endpoints

```csharp
var mock = this.CreateGitHubMock(opt => opt.Port = 0);

// Use built-in defaults for specific resources
mock.Repositories().Get().UseDefault();
mock.PullRequests().List().UseDefault();
mock.Issues().Create().UseDefault();

// Override with custom responses
mock.Users().Get().WithResponse(new
{
    login = "testuser",
    id = 12345,
    type = "User",
    name = "Test User"
});

await mock.StartAsync();
```

### Use from a generic simulator

```csharp
using Augustus.Extensions;
using Augustus.APIs.GitHub;

var simulator = this.CreateAPISimulator("GitHub", opt => opt.Port = 0);
simulator.GitHub().Repositories().Get().UseDefault();
simulator.GitHub().PullRequests().List().UseDefault();

await simulator.StartAsync();
```

## Key Features

- **One-line setup** — `UseAllDefaults()` registers all GitHub endpoints with realistic responses
- **Fluent resource builders** — type-safe API for configuring individual GitHub resources
- **Realistic defaults** — built-in responses that match GitHub's actual REST API shape
- **Custom overrides** — replace any default with inline JSON, objects, or file-based responses
- **Error simulation** — return pre-configured GitHub error responses (404, 422, etc.)
- **Latency simulation** — add realistic latency with `WithLatency(meanMs, stdDevMs)`

## Supported Resources

| Builder | Endpoints |
|---------|-----------|
| `Repositories()` | Get, ListForAuthenticatedUser, ListForUser, Create, Update, Delete |
| `Issues()` | Get, List, Create, Update |
| `PullRequests()` | Get, List, Create, Update |
| `Commits()` | Get, List |
| `Branches()` | Get, List |
| `Releases()` | Get, List, Create |
| `Actions()` | GetWorkflowRun, ListWorkflowRuns |
| `Users()` | Get, GetAuthenticated |
| `Organizations()` | Get, ListForAuthenticatedUser, ListForUser |
| `GitRefs()` | Get, List, Create |
| `Search()` | Repositories, Issues, Code |

## Related Packages

| Package | Purpose |
|---------|---------|
| [Augustus](https://www.nuget.org/packages/Augustus) | Core simulator — route matching, static responses, caching |
| [Augustus.AI](https://www.nuget.org/packages/Augustus.AI) | AI-powered response generation and real-API proxy via OpenAI |
| [Augustus.Stripe](https://www.nuget.org/packages/Augustus.Stripe) | Pre-built Stripe API defaults and fluent helpers |
| [Augustus.Reqnroll](https://www.nuget.org/packages/Augustus.Reqnroll) | Reqnroll (BDD) integration with per-scenario cache isolation |

## Documentation

Full documentation and examples: [github.com/chrisjainsley/augustus](https://github.com/chrisjainsley/augustus)
