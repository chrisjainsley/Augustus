using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Augustus.AI;

/// <summary>
/// Handles OpenAI API requests with retry logic, exponential backoff, and request queuing.
/// </summary>
internal class OpenAIRequestHandler
{
    private readonly OpenAIClient openAiClient;
    private readonly AIOptions options;
    private readonly SemaphoreSlim requestSemaphore;

    public OpenAIRequestHandler(OpenAIClient openAiClient, AIOptions options)
    {
        this.openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));

        requestSemaphore = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
    }

    public async Task<ClientResult<ChatCompletion>> CompleteChatWithRetryAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        await requestSemaphore.WaitAsync(cancellationToken);

        try
        {
            return await ExecuteWithRetryAsync(messages, cancellationToken);
        }
        finally
        {
            requestSemaphore.Release();
        }
    }

    private async Task<ClientResult<ChatCompletion>> ExecuteWithRetryAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var chatClient = openAiClient.GetChatClient(options.OpenAIModel);
        int attemptCount = 0;
        int delayMilliseconds = options.InitialRetryDelayMs;

        while (true)
        {
            attemptCount++;

            try
            {
                return await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
            }
            catch (ClientResultException ex) when (ShouldRetry(ex, attemptCount))
            {
                LogRetryAttempt(attemptCount, ex, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
            catch (TaskCanceledException) when (attemptCount <= options.MaxRetries)
            {
                LogRetryAttempt(attemptCount, new Exception("Request timeout"), delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
            catch (HttpRequestException ex) when (ShouldRetryHttpException(ex, attemptCount))
            {
                LogRetryAttempt(attemptCount, ex, delayMilliseconds);
                await Task.Delay(delayMilliseconds, cancellationToken);
                delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
            }
        }
    }

    private bool ShouldRetry(ClientResultException exception, int attemptCount)
    {
        if (attemptCount >= options.MaxRetries)
            return false;

        // HTTP 429 (rate limit) or 500-504 (server errors)
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
}
