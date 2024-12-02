using System.Diagnostics;

namespace Carubbi.DataSinks.Tests;

public class DataSinkTests
{
    [Fact]
    public async Task BatchingDataSink()
    {
        var batchingSink = new BatchingDataSink<string>(
            batchSize: 5,
            timeLimit: TimeSpan.FromSeconds(10),
            processBatch: async batch =>
            {
                Debug.WriteLine($"Batch: {string.Join(", ", batch)}");
                await Task.Delay(200);
            });


        var counter = 0;
        while (counter < 10)
        {
            await batchingSink.ProcessAsync($"item{counter}");
            Debug.WriteLine($"item{counter} added to data sink");
            await Task.Delay(200 * counter);
            counter++;
        }
        await batchingSink.CompleteAsync();
    }

    [Fact]
    public async Task DelayedDataSink()
    {

        var delayedSink = new DelayedDataSink<string>(
            delay: TimeSpan.FromSeconds(3),
            process: async item =>
            {
                Debug.WriteLine($"Processed with delay: {item}");
                await Task.Delay(100);
            });

        var counter = 0;
        while (counter < 10)
        {

            await delayedSink.ProcessAsync($"item{counter}");
            Debug.WriteLine($"item{counter} added to data sink");
            await Task.Delay(200 * counter);
            counter++;
        }
        await delayedSink.CompleteAsync();
    }

    [Fact]
    public async Task BufferedDataSink()
    {

        var bufferedSink = new BufferedDataSink<string>(
            maxWorkers: 3,
            process: async item =>
            {
                Debug.WriteLine($"Processed by worker: {item}");
                await Task.Delay(300);
            });

        var counter = 0;
        while (counter < 10)
        {
            for (int i = 0; i <= counter; i++)
            {
                await bufferedSink.ProcessAsync($"item{counter}");
                Debug.WriteLine($"item{counter} added to data sink");
            }
            await Task.Delay(200 * counter);
            counter++;
        }
        await bufferedSink.CompleteAsync();
    }


    [Fact]
    public async Task DynamicBufferedDataSink()
    {

        var bufferedSink = new DynamicBufferedDataSink<string>(
            minWorkers: 3,
            maxWorkers: 6,
            scalingFactor: 4,
            process: async item =>
            {
                Debug.WriteLine($"Processed by worker: {item}");
                await Task.Delay(300);
            });

        var counter = 0;
        while (counter < 50)
        {
            for (int i = 0; i <= counter; i++)
            {
                await bufferedSink.ProcessAsync($"item{counter}");
                Debug.WriteLine($"item{counter} added to data sink");
            }
            await Task.Delay(200 * counter);
            counter++;
        }
        await bufferedSink.CompleteAsync();
    }
}