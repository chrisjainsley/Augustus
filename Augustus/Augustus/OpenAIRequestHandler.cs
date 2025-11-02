using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace Augustus
{
    /// <summary>
    /// Handles OpenAI API requests with retry logic, exponential backoff, and request queuing.
    /// </summary>
    /// <remarks>
    /// This class provides resilient OpenAI API integration by:
    /// <list type="bullet">
    /// <item><description>Implementing exponential backoff for rate-limited requests</description></item>
    /// <item><description>Queuing requests to prevent overwhelming the API</description></item>
    /// <item><description>Automatically retrying transient failures</description></item>
    /// <item><description>Configurable retry limits and delay settings</description></item>
    /// </list>
    /// </remarks>
    internal class OpenAIRequestHandler
    {
        private readonly OpenAIClient openAiClient;
        private readonly APISimulatorOptions options;
        private readonly SemaphoreSlim requestSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenAIRequestHandler"/> class.
        /// </summary>
        /// <param name="openAiClient">The OpenAI client instance.</param>
        /// <param name="options">Configuration options including retry and concurrency settings.</param>
        /// <exception cref="ArgumentNullException">Thrown if openAiClient or options is null.</exception>
        public OpenAIRequestHandler(OpenAIClient openAiClient, APISimulatorOptions options)
        {
            this.openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
            this.options = options ?? throw new ArgumentNullException(nameof(options));

            // Create semaphore to limit concurrent requests
            requestSemaphore = new SemaphoreSlim(options.MaxConcurrentRequests, options.MaxConcurrentRequests);
        }

        /// <summary>
        /// Executes a chat completion request with retry logic and queuing.
        /// </summary>
        /// <param name="messages">The chat messages to send to OpenAI.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The chat completion result from OpenAI.</returns>
        /// <exception cref="ClientResultException">Thrown if the request fails after all retry attempts.</exception>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
        public async Task<ClientResult<ChatCompletion>> CompleteChatWithRetryAsync(
            IEnumerable<ChatMessage> messages,
            CancellationToken cancellationToken = default)
        {
            // Wait for available slot in the request queue
            await requestSemaphore.WaitAsync(cancellationToken);

            try
            {
                return await ExecuteWithRetryAsync(messages, cancellationToken);
            }
            finally
            {
                // Release the semaphore slot
                requestSemaphore.Release();
            }
        }

        /// <summary>
        /// Executes the OpenAI API call with exponential backoff retry logic.
        /// </summary>
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
                    // Attempt the API call
                    return await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                }
                catch (ClientResultException ex) when (ShouldRetry(ex, attemptCount))
                {
                    // Log the retry attempt
                    LogRetryAttempt(attemptCount, ex, delayMilliseconds);

                    // Wait before retrying with exponential backoff
                    await Task.Delay(delayMilliseconds, cancellationToken);

                    // Double the delay for next retry, but cap at maximum
                    delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
                }
                catch (TaskCanceledException) when (attemptCount < options.MaxRetries)
                {
                    // Timeout occurred, retry if we haven't exceeded max retries
                    LogRetryAttempt(attemptCount, new Exception("Request timeout"), delayMilliseconds);

                    await Task.Delay(delayMilliseconds, cancellationToken);
                    delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
                }
                catch (HttpRequestException ex) when (ShouldRetryHttpException(ex, attemptCount))
                {
                    // Network errors or transient HTTP errors
                    LogRetryAttempt(attemptCount, ex, delayMilliseconds);

                    await Task.Delay(delayMilliseconds, cancellationToken);
                    delayMilliseconds = Math.Min(delayMilliseconds * 2, options.MaxRetryDelayMs);
                }
                // If we reach here without returning or retrying, the exception will propagate
            }
        }

        /// <summary>
        /// Determines if a ClientResultException should be retried.
        /// </summary>
        private bool ShouldRetry(ClientResultException exception, int attemptCount)
        {
            if (attemptCount >= options.MaxRetries)
            {
                return false;
            }

            // Check for rate limit errors (HTTP 429)
            if (exception.Status == 429)
            {
                return true;
            }

            // Check for transient server errors (500, 502, 503, 504)
            if (exception.Status >= 500 && exception.Status <= 504)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if an HttpRequestException should be retried.
        /// </summary>
        private bool ShouldRetryHttpException(HttpRequestException exception, int attemptCount)
        {
            if (attemptCount >= options.MaxRetries)
            {
                return false;
            }

            // Retry on network errors (connection failures, timeouts, etc.)
            return true;
        }

        /// <summary>
        /// Logs retry attempt information to the console.
        /// </summary>
        private void LogRetryAttempt(int attemptCount, Exception exception, int delayMs)
        {
            Console.WriteLine($"[OpenAI Retry] Attempt {attemptCount}/{options.MaxRetries} failed: {exception.Message}. Retrying in {delayMs}ms...");
        }
    }
}
