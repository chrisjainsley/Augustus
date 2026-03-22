# Augustus Stripe sample tests

## Local (default)

With `OPENAI_API_KEY` or user secrets (`OpenAI:ApiKey`), these tests drive the Stripe simulator with the AI default handler and read/write cache under `__mocks__/{TestClass}/Stripe/`.

## CI cache-only mode

When the environment variable `AUGUSTUS_STRIPE_SAMPLE_CI_CACHE_ONLY` is set to `1` or `true` (as in the `stripe-sample` GitHub Actions job), tests run with `CacheOnly` and **no API key**. Responses must come from committed JSON files in `__mocks__`.

If cache-key inputs change (HTTP method/path/query, normalized body, or instruction text), update or add the corresponding `{RequestHash}.json` entry. Regenerate mocks by running the tests locally with an API key and copying the produced cache files into `__mocks__` using the SHA-256 filename reported on a cache miss (HTTP 503) in cache-only mode.
