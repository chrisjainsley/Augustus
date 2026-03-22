using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Augustus.AI;

/// <summary>
/// Handles OpenAI API requests with shared process-wide throttling, optional deduplication by cache key,
/// retry logic with <c>Retry-After</c> awareness, exponential backoff, and jitter.
/// </summary>
internal class OpenAIRequestHandler : IDisposable
{
    private readonly OpenAIClient openAiClient;
    private readonly AIOptions options;
    private readonly OpenAICallCoordinator coordinator;

    public OpenAIRequestHandler(OpenAIClient openAiClient, AIOptions options)
    {
        this.openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        coordinator = OpenAICallCoordinator.GetOrCreate(options);
    }

    /// <param name="dedupeKey">When non-empty, concurrent calls with the same key share one OpenAI execution (typically the cache request hash).</param>
    public Task<ClientResult<ChatCompletion>> CompleteChatWithRetryAsync(
        string dedupeKey,
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? chatOptions = null,
        CancellationToken cancellationToken = default)
    {
        return coordinator.ExecuteAsync(
            string.IsNullOrEmpty(dedupeKey) ? null : dedupeKey,
            ct => ExecuteWithRetryAsync(messages, chatOptions, ct),
            cancellationToken);
    }

    private async Task<ClientResult<ChatCompletion>> ExecuteWithRetryAsync(
        IEnumerable<ChatMessage> messages,
        ChatCompletionOptions? chatOptions,
        CancellationToken cancellationToken)
    {
        var modelOrDeployment = options.UseAzureOpenAI
            ? options.AzureDeploymentName
            : options.OpenAIModel;
        var chatClient = openAiClient.GetChatClient(modelOrDeployment);
        var attemptCount = 0;
        var delayMilliseconds = options.InitialRetryDelayMs;

        while (true)
        {
            attemptCount++;

            try
            {
                return await chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ClientResultException ex) when (ShouldRetry(ex, attemptCount))
            {
                delayMilliseconds = OpenAIRetryDelays.ComputeNextDelayMs(options, delayMilliseconds, ex);
                LogRetryAttempt(attemptCount, ex, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
            catch (TaskCanceledException) when (attemptCount <= options.MaxRetries)
            {
                delayMilliseconds = OpenAIRetryDelays.ComputeNextDelayMs(options, delayMilliseconds, clientException: null);
                LogRetryAttempt(attemptCount, new Exception("Request timeout"), delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
            catch (HttpRequestException ex) when (ShouldRetryHttpException(ex, attemptCount))
            {
                delayMilliseconds = OpenAIRetryDelays.ComputeNextDelayMs(options, delayMilliseconds, clientException: null);
                LogRetryAttempt(attemptCount, ex, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken).ConfigureAwait(false);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
        }
    }

    private bool ShouldRetry(ClientResultException exception, int attemptCount)
    {
        if (attemptCount >= options.MaxRetries)
            return false;

        return exception.Status == 429 || (exception.Status >= 500 && exception.Status <= 504);
    }

    private bool ShouldRetryHttpException(HttpRequestException exception, int attemptCount)
    {
        return attemptCount < options.MaxRetries;
    }

    private void LogRetryAttempt(int attemptCount, Exception exception, int delayMs)
    {
        Console.WriteLine($"[OpenAI Retry] Attempt {attemptCount}/{options.MaxRetries} failed: {exception.Message}. Retrying in {delayMs}ms...");
    }

    /// <summary>
    /// Disposes the OpenAI request handler. Shared coordination resources are not disposed per handler.
    /// </summary>
    public void Dispose()
    {
    }
}
