# Augustus Performance Guardian Memory

## Project Overview
- Augustus is an AI-powered HTTP API simulator for .NET integration tests
- It runs a local ASP.NET Core proxy that intercepts requests and either serves cached responses or calls OpenAI
- Package: Augustus.AI v0.2.x, targets net6-net9

## Key Files
- `Augustus/Augustus/OpenAIRequestHandler.cs` — SemaphoreSlim concurrency gate to OpenAI
- `Augustus/Augustus/ResponseGenerator.cs` — Core request pipeline (cache check → OpenAI → cache write)
- `Augustus/Augustus/FileManager.cs` — Async file I/O for cache read/write
- `Augustus/Augustus/WebHost.cs` — ASP.NET Core host, single `app.Run(...)` middleware
- `Augustus/Augustus/APISimulatorOptions.cs` — Config including `MaxConcurrentRequests` (default 10)
- `Augustus/Augustus/SensitiveDataSanitizer.cs` — 5x compiled Regex applied on every cache write

## Confirmed Performance Bottlenecks (Issue #37)

### CRITICAL
1. **Single port = single simulator instance per test** — each `APISimulator` binds one port; xUnit Reqnroll tests likely create one simulator per test class and run tests within a class SERIALLY (xUnit default). Each request in an agentic tool_call loop waits for the prior OpenAI response before the next request arrives. 180 requests / ~13s average gpt-5-mini latency ≈ 39 min when effectively serial.

### HIGH
2. **`new JsonSerializerOptions { WriteIndented = true }` allocated per cache write** — `FileManager.cs:83`. JsonSerializerOptions construction is expensive (~10ms). Should be a static readonly field.
3. **5 sequential regex passes on every cache write** — `SensitiveDataSanitizer.cs` applies 5 compiled regexes to both the request body and each instruction string on every cache write. For long prompts/responses this multiplies per request.
4. **`requestSemaphore` with MaxConcurrentRequests=10** — NOT the bottleneck within a single simulator since agentic tests serialize requests themselves, but confirms there is no artificial 1-at-a-time limit from the semaphore at default settings.

### MEDIUM
5. **No `ConfigureAwait(false)` anywhere** — library code should use `ConfigureAwait(false)` on all internal awaits to avoid ASP.NET context capture overhead.
6. **`new HttpClient()` per `CreateClient()` call** — callers get a fresh HttpClient every call; no connection pooling/reuse enforced.

## Architecture Notes
- The proxy pipeline is fully async (no `.Result`/`.Wait()` blocking found)
- Cache reads and writes use `File.ReadAllTextAsync`/`File.WriteAllTextAsync` — correctly async
- The SemaphoreSlim in OpenAIRequestHandler gates outbound OpenAI calls (not inbound proxy requests)
- The SemaphoreSlim in WebHost only guards start/stop lifecycle, not request handling
- xUnit v2 (used in Augustus.Tests) runs tests within a class serially by default; parallelism is across classes in separate collections
