using System.Collections.Concurrent;

namespace Carubbi.DataSinks;
public class ElasticWorkerDataSink<T> : IDataSink<T>
{
    private readonly Func<T, Task> _process;
    private readonly int _maxWorkers;
    private readonly int _minWorkers;
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly ConcurrentDictionary<Task, CancellationTokenSource> _workerTokens = new();
    private readonly SemaphoreSlim _workerSemaphore;
    private readonly CancellationTokenSource _globalCts = new();
    private int _currentWorkerCount = 0;
    private double _scalingFactor;

    public ElasticWorkerDataSink(int minWorkers, int maxWorkers, double scalingFactor, Func<T, Task> process)
    {
        if (minWorkers <= 0 || maxWorkers < minWorkers)
            throw new ArgumentException("Invalid worker limits.");

        _minWorkers = minWorkers;
        _maxWorkers = maxWorkers;
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _workerSemaphore = new SemaphoreSlim(maxWorkers);
        _scalingFactor = scalingFactor;
        // Start the minimum number of workers initially
        for (int i = 0; i < _minWorkers; i++)
        {
            AddWorker();
        }
    }

    public Task ProcessAsync(T data)
    {
        if (_globalCts.IsCancellationRequested)
            throw new InvalidOperationException("Cannot process new data after CompleteAsync has been called.");

        _queue.Enqueue(data);

        
        AdjustWorkerPool();
        return Task.CompletedTask;
    }

    private void AdjustWorkerPool()
    {
        
        if (_queue.Count > _currentWorkerCount * _scalingFactor && _currentWorkerCount < _maxWorkers)
        {
            AddWorker();
             
        }
  
        else if (_queue.Count < _currentWorkerCount / _scalingFactor && _currentWorkerCount > _minWorkers)
        {
            RemoveWorker();
          
        }
    }

    private void AddWorker()
    {
        if (Interlocked.Increment(ref _currentWorkerCount) > _maxWorkers)
        {
            Interlocked.Decrement(ref _currentWorkerCount);  
            return;
        }

 
        var workerCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
        var workerTask = Task.Run(async () => await WorkerLoop(workerCts.Token));

     
        _workerTokens.TryAdd(workerTask, workerCts);
    }

    private void RemoveWorker()
    {
        if (Interlocked.Decrement(ref _currentWorkerCount) < _minWorkers)
        {
            Interlocked.Increment(ref _currentWorkerCount); // Revert if under the minimum
            return;
        }

     
        if (_workerTokens.TryRemove(_workerTokens.Keys.FirstOrDefault()!, out var workerCts))
        {
            workerCts.Cancel();  
            workerCts.Dispose();  
        }
    }

    private async Task WorkerLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (_queue.TryDequeue(out var item))
                {
                    await _workerSemaphore.WaitAsync(token);
                    try
                    {
                        await _process(item);
                    }
                    finally
                    {
                        _workerSemaphore.Release();
                    }
                }
                else
                {
                    
                    await Task.Delay(50, token);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the worker is removed or global cancellation occurs
        }
    }

    public async Task CompleteAsync()
    {
        _globalCts.Cancel(); 

     
        while (!_queue.IsEmpty)
        {
            await Task.Delay(100);
        }

      
        foreach (var worker in _workerTokens)
        {
            worker.Value.Cancel();
            worker.Value.Dispose();
        }

        
        await Task.WhenAll(_workerTokens.Keys);
    }
}
