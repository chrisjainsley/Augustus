using System.Collections.Concurrent;

namespace Augustus;

internal class BackgroundCacheWriter
{
    private readonly ConcurrentDictionary<int, Task> _pendingWrites = new();
    private int _writeId;

    public void Enqueue(Func<Task> writeAction)
    {
        var id = Interlocked.Increment(ref _writeId);
        var task = RunAsync(id, writeAction);
        _pendingWrites.TryAdd(id, task);
    }

    private async Task RunAsync(int id, Func<Task> writeAction)
    {
        try
        {
            await writeAction().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warning: Failed to cache response: {ex.Message}");
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
