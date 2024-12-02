using System.Collections.Concurrent;

namespace Carubbi.DataSinks;


public class BufferedDataSink<T>(int maxWorkers, Func<T, Task> process) : IDataSink<T>
{
    private readonly Func<T, Task> _process = process ?? throw new ArgumentNullException(nameof(process));
    private readonly int _maxWorkers = maxWorkers;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _workerSemaphore = new(maxWorkers);

    private volatile bool _isCompleting = false;

    public async Task ProcessAsync(T data)
    {
        if (_isCompleting) throw new InvalidOperationException("Cannot process after CompleteAsync has been called.");

        _queue.Enqueue(data);


        if (_workerSemaphore.CurrentCount > 0)
        {
            await StartWorkerAsync();
        }
    }

    private async Task StartWorkerAsync()
    {
        await _workerSemaphore.WaitAsync();

        _ = Task.Run(async () =>
        {
            try
            {
                while (_queue.TryDequeue(out var item))
                {
                    await _process(item);
                }
            }
            finally
            {
                _workerSemaphore.Release();
            }
        });
    }

    public async Task CompleteAsync()
    {
        _isCompleting = true;

        while (!_queue.IsEmpty)
        {
            await Task.Delay(100);
        }


        for (int i = 0; i < _maxWorkers; i++)
        {
            await _workerSemaphore.WaitAsync();
        }
    }
}
