using System.Collections.Concurrent;

namespace Carubbi.DataSinks;

public class BatchingDataSink<T> : IDataSink<T>, IDisposable
{
    private readonly int _batchSize;
    private readonly Func<IEnumerable<T>, Task> _processBatch;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly Timer _timer;
    private readonly SemaphoreSlim _batchSemaphore = new(1);

    public BatchingDataSink(int batchSize, TimeSpan timeLimit, Func<IEnumerable<T>, Task> processBatch)
    {
        _batchSize = batchSize;
        _processBatch = processBatch ?? throw new ArgumentNullException(nameof(processBatch));
        _timer = new Timer(OnTimeLimitReached, null, timeLimit, timeLimit);
    }

    public async Task ProcessAsync(T data)
    {
        _queue.Enqueue(data);


        if (_queue.Count >= _batchSize)
        {
            await TryFlushAsync();
        }
    }

    public async Task CompleteAsync()
    {
        await TryFlushAsync();
        _timer.Dispose();
    }

    private async void OnTimeLimitReached(object? state)
    {
        await TryFlushAsync();
    }

    private async Task TryFlushAsync()
    {
        if (!_batchSemaphore.Wait(0)) return;

        try
        {
            var items = new List<T>();
            while (items.Count < _batchSize && _queue.TryDequeue(out var item))
            {
                items.Add(item);
            }

            if (items.Count > 0)
            {
                await _processBatch(items);
            }
        }
        finally
        {
            _batchSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }
}
