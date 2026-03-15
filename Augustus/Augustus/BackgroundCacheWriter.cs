using System.Collections.Concurrent;

namespace Augustus;

internal class BackgroundCacheWriter
{
    private readonly APISimulator.FileManager _fileManager;
    private readonly ConcurrentDictionary<int, Task> _pendingWrites = new();
    private int _writeId;

    public BackgroundCacheWriter(APISimulator.FileManager fileManager)
    {
        _fileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
    }

    public void Enqueue(string requestHash, string responseContent, string originalRequest, List<string> instructions)
    {
        var id = Interlocked.Increment(ref _writeId);
        var task = WriteAsync(id, requestHash, responseContent, originalRequest, instructions);
        _pendingWrites.TryAdd(id, task);
    }

    private async Task WriteAsync(int id, string requestHash, string responseContent, string originalRequest, List<string> instructions)
    {
        try
        {
            await _fileManager.CacheResponseAsync(requestHash, responseContent, originalRequest, instructions).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to cache response: {ex.Message}");
        }
        finally
        {
            _pendingWrites.TryRemove(id, out _);
        }
    }

    public async Task DrainAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _pendingWrites.Values.ToArray();
        if (tasks.Length > 0)
        {
            var allTasks = Task.WhenAll(tasks);
            var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            await Task.WhenAny(allTasks, cancellationTask).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
