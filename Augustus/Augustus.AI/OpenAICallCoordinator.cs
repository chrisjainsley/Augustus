using System.ClientModel;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using OpenAI.Chat;

namespace Augustus.AI;

/// <summary>
/// Process-wide coordination for OpenAI chat calls: shared concurrency limit and optional in-flight deduplication by cache key.
/// </summary>
internal sealed class OpenAICallCoordinator
{
    private static readonly ConcurrentDictionary<string, OpenAICallCoordinator> Instances = new(StringComparer.Ordinal);

    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<string, Lazy<Task<ClientResult<ChatCompletion>>>> _inFlight = new(StringComparer.Ordinal);

    private OpenAICallCoordinator(int maxConcurrentRequests)
    {
        _semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
    }

    /// <summary>
    /// Gets or creates a coordinator for the given credentials and model/deployment.
    /// <see cref="AIOptions.MaxConcurrentRequests"/> is applied only on first creation for that key.
    /// </summary>
    public static OpenAICallCoordinator GetOrCreate(AIOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var key = BuildInstanceKey(options);
        return Instances.GetOrAdd(key, _ => new OpenAICallCoordinator(options.MaxConcurrentRequests));
    }

    /// <summary>Exposed for tests: coordinator identity includes credentials, model, and <see cref="AIOptions.MaxConcurrentRequests"/>.</summary>
    internal static string BuildInstanceKey(AIOptions o)
    {
        var sb = new StringBuilder(256);
        sb.Append(o.UseAzureOpenAI ? 'A' : 'O');
        sb.Append('|');
        sb.Append(o.OpenAIApiKey);
        sb.Append('|');
        sb.Append(o.OpenAIEndpoint);
        sb.Append('|');
        sb.Append(o.UseAzureOpenAI ? o.AzureDeploymentName : o.OpenAIModel);
        sb.Append('|');
        sb.Append(o.MaxConcurrentRequests);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    /// <summary>
    /// Runs <paramref name="operation"/> under the shared semaphore. When <paramref name="dedupeKey"/> is non-empty,
    /// concurrent callers with the same key share a single <paramref name="operation"/> execution.
    /// </summary>
    public async Task<ClientResult<ChatCompletion>> ExecuteAsync(
        string? dedupeKey,
        Func<CancellationToken, Task<ClientResult<ChatCompletion>>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (string.IsNullOrEmpty(dedupeKey))
        {
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await operation(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        var lazy = _inFlight.GetOrAdd(
            dedupeKey,
            _ => new Lazy<Task<ClientResult<ChatCompletion>>>(
                () => RunDedupedOnceAsync(dedupeKey, operation),
                LazyThreadSafetyMode.ExecutionAndPublication));

        return await lazy.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ClientResult<ChatCompletion>> RunDedupedOnceAsync(
        string dedupeKey,
        Func<CancellationToken, Task<ClientResult<ChatCompletion>>> operation)
    {
        await _semaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            return await operation(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
            _inFlight.TryRemove(dedupeKey, out _);
        }
    }
}
