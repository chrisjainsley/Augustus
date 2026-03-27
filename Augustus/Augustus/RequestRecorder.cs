namespace Augustus;

using System.Collections.Concurrent;

/// <summary>
/// Thread-safe recorder that captures incoming HTTP requests for later verification.
/// </summary>
internal class RequestRecorder
{
    private readonly ConcurrentQueue<RecordedRequest> requests = new();

    public void Record(RecordedRequest request)
    {
        requests.Enqueue(request);
    }

    public IReadOnlyList<RecordedRequest> GetRecordedRequests()
    {
        return requests.ToArray();
    }

    public void Clear()
    {
        requests.Clear();
    }
}
