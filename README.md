# Carubbi.DataSinks

**Carubbi.DataSinks** is a .NET library providing reusable and robust implementations of data sinks to simplify and optimize data processing workflows. Whether you need batch processing, delayed handling, or concurrency management, this library offers ready-to-use solutions.

## 📦 Installation

You can add the library to your project using one of the following methods:

### Package Manager
\```bash
Install-Package Carubbi.DataSinks
\```

### .NET CLI
\```bash
dotnet add package Carubbi.DataSinks
\```

---

## 🚀 Features

- **Batching Data Sink**: Processes data in configurable batches (size and time-based).
- **Delayed Data Sink**: Delays processing of each item by a specified timeout.
- **Buffered Data Sink**: Manages high-concurrency workflows with worker pools and buffering.

---

## 🛠 Usage

### 1. Batching Data Sink

Processes items in batches based on size or time limits.

\```csharp
var batchingSink = new BatchingDataSink<string>(
    batchSize: 10,
    timeLimit: TimeSpan.FromSeconds(5),
    processBatch: async batch =>
    {
        Console.WriteLine($"Processing batch: {string.Join(", ", batch)}");
        await Task.Delay(100);
    });

await batchingSink.ProcessAsync("Item1");
await batchingSink.ProcessAsync("Item2");
// Add more items...
await batchingSink.CompleteAsync();
\```

---

### 2. Delayed Data Sink

Delays processing of each item for a configurable amount of time.

\```csharp
var delayedSink = new DelayedDataSink<string>(
    delay: TimeSpan.FromSeconds(3),
    process: async item =>
    {
        Console.WriteLine($"Processed after delay: {item}");
        await Task.Delay(100);
    });

await delayedSink.ProcessAsync("ItemA");
await delayedSink.ProcessAsync("ItemB");
await delayedSink.CompleteAsync();
\```

---

### 3. Buffered Data Sink

Manages concurrency with a configurable number of workers.

\```csharp
var bufferedSink = new BufferedDataSink<string>(
    maxWorkers: 3,
    process: async item =>
    {
        Console.WriteLine($"Processed by worker: {item}");
        await Task.Delay(300);
    });

await bufferedSink.ProcessAsync("Task1");
await bufferedSink.ProcessAsync("Task2");
await bufferedSink.CompleteAsync();
\```

---

## 🔧 Configuration

Each data sink can be customized to suit specific needs:

- **Batching Data Sink**:
  - `batchSize`: Maximum number of items in a batch.
  - `timeLimit`: Maximum time to wait before processing a batch.

- **Delayed Data Sink**:
  - `delay`: Time to delay processing each item.

- **Buffered Data Sink**:
  - `maxWorkers`: Number of concurrent workers for processing.

---

## 🤝 Contributing

Contributions are welcome! If you find a bug or want to suggest a feature, feel free to open an issue or submit a pull request.

---

## ⚖️ License

This library is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

## 🙌 Acknowledgments

Special thanks to everyone contributing to modernizing data processing patterns in .NET.
