# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test

```bash
dotnet build Augustus/Augustus.sln
dotnet test Augustus/Augustus.sln
dotnet test Augustus/Augustus.sln -f net9.0  # single framework
```

Projects in this repo target net6.0, net7.0, net8.0, net9.0, and net10.0 (varies by project). CI runs tests for each project's target frameworks; for local dev, testing on one framework is usually sufficient.

## Testing Conventions

- xUnit with FluentAssertions for assertions and Verify.Xunit for snapshot testing
- PublicApiGenerator for API surface verification
- Test projects mirror source projects: `Augustus.Tests`, `Augustus.AI.Tests`, etc.
- Samples include Reqnroll (BDD/Gherkin) specs

## Environment Setup

- `OPENAI_API_KEY`: Required for AI samples. Set via `dotnet user-secrets` or environment variable.
- `AUGUSTUS_STRIPE_SAMPLE_CI_CACHE_ONLY`: Set in CI to run Stripe sample tests in cache-only mode.

## Code Style

- ImplicitUsings and Nullable enabled across all projects
- Seal classes when appropriate for public API hardening
- Follow existing patterns — do not introduce new abstractions without justification
- Review standards detailed in @.github/copilot-instructions.md

## Workflow

- Use TDD: write failing tests first, then implement the minimum code to make them pass, then refactor
- After completing implementation and before running the full test suite, run `/simplify` to review for reuse, quality, and efficiency, then run `/dotnet-skills:csharp-api-design` to review API design
- PR templates in `.github/PULL_REQUEST_TEMPLATE/` — choose `feature.md`, `bug.md`, or `chore.md`
- Branch naming: semantic format (e.g., `feature/oauth-support`, `bugfix/fix-cache-race-condition`)

## Project Structure

- `Augustus/Augustus/` — Core API simulation library
- `Augustus/Augustus.AI/` — OpenAI/Azure OpenAI integration
- `Augustus/Augustus.APIs.Stripe/` — Pre-built Stripe API defaults
- `Augustus/Augustus.Reqnroll/` — BDD/Reqnroll integration
- `Augustus/samples/` — Example projects and integration tests
