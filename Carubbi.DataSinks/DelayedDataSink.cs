using System.Collections.Concurrent;

namespace Carubbi.DataSinks;

public class DelayedDataSink<T>(TimeSpan delay, Func<T, Task> process) : IDataSink<T>
{
    private readonly Func<T, Task> _process = process ?? throw new ArgumentNullException(nameof(process));
    private readonly TimeSpan _delay = delay;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _workerSemaphore = new(1, 1);
    private volatile bool _isCompleting = false;

    public async Task ProcessAsync(T data)
    {
        if (_isCompleting)
            throw new InvalidOperationException("Cannot process data after CompleteAsync has been called.");

        _queue.Enqueue(data);


        await StartWorkerIfNotRunningAsync();
    }

    private async Task StartWorkerIfNotRunningAsync()
    {

        if (_workerSemaphore.CurrentCount > 0)
        {
            await _workerSemaphore.WaitAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (!_isCompleting || !_queue.IsEmpty)
                    {
                        if (_queue.TryDequeue(out var item))
                        {
                            await Task.Delay(_delay);
                            await _process(item);
                        }
                        else
                        {
                            await Task.Delay(50);
                        }
                    }
                }
                finally
                {
                    _workerSemaphore.Release();
                }
            });
        }
    }

    public async Task CompleteAsync()
    {
        _isCompleting = true;


        while (!_queue.IsEmpty)
        {
            await Task.Delay(100);
        }


        await _workerSemaphore.WaitAsync();
        _workerSemaphore.Release();
    }
}
