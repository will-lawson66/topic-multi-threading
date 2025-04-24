# Module 1: Thread Fundamentals

Let's start with the essential foundations of multi-threading in C#. Understanding these concepts will give you the necessary foundation for building robust multi-threaded applications for robotics.

## 1. Creating and Starting Threads

In C#, threads are represented by the System.Threading.Thread class. Here's a simple example of creating and starting a thread:

```csharp
using System;
using System.Threading;
class Program
{
    static void Main()
    {
        // Create a thread with a ThreadStart delegate
        Thread thread = new Thread(new ThreadStart(DoWork));
        
        // Set thread properties before starting
        thread.Name = "WorkerThread";
        thread.IsBackground = true;
        
        // Start the thread
        thread.Start();
        
        Console.WriteLine("Main thread continues execution...");
        
        // Wait for the worker thread to complete
        thread.Join();
        
        Console.WriteLine("Worker thread has completed.");
    }
    
    static void DoWork()
    {
        Console.WriteLine($"Thread {Thread.CurrentThread.Name} is working...");
        Thread.Sleep(2000); // Simulate work
        Console.WriteLine($"Thread {Thread.CurrentThread.Name} has finished work.");
    }
}
```

## 2. Thread Lifecycle and States

Threads go through several states during their lifetime:
- **Unstarted**: When a thread is created but Start() hasn't been called yet
- **Running**: When the thread is executing
- **WaitSleepJoin**: When a thread is blocked due to Wait(), Sleep(), or Join() calls
- **Suspended**: When a thread is temporarily halted (not used in modern code)
- **Stopped**: When the thread has completed execution

You can check the thread state using the ThreadState property:

```csharp
Thread thread = new Thread(DoWork);
Console.WriteLine(thread.ThreadState); // Unstarted
thread.Start();
Console.WriteLine(thread.ThreadState); // Running (or WaitSleepJoin if already blocked)
```

## 3. Thread Properties

Several important properties control thread behavior:

```csharp
Thread thread = new Thread(DoWork);
// Name - for debugging purposes
thread.Name = "DataProcessingThread";
// IsBackground - determines if the thread keeps the application running
thread.IsBackground = true; // Application will exit even if this thread is still running
// Priority - influences scheduling (use with caution)
thread.Priority = ThreadPriority.Normal; // Options: Lowest, BelowNormal, Normal, AboveNormal, Highest
```

## 4. Passing Data to Threads

You can pass data to a thread in several ways:

Using ParameterizedThreadStart:

```csharp
static void Main()
{
    Thread thread = new Thread(new ParameterizedThreadStart(ProcessData));
    thread.Start(42); // Pass an integer value
}
static void ProcessData(object data)
{
    int value = (int)data;
    Console.WriteLine($"Processing value: {value}");
}
```

Using closure:

```csharp
static void Main()
{
    int value = 42;
    string message = "Hello";
    
    Thread thread = new Thread(() => 
    {
        // notice how the anonymous method had access to the containing scope - this is closure
        Console.WriteLine($"Value: {value}, Message: {message}");
    });
    thread.Start();
}
```

## 5. Thread Synchronization Basics

When multiple threads access shared data, you need synchronization to prevent race conditions:

```csharp
class Counter
{
    private int _count = 0;
    private readonly object _lock = new object();
    
    public void Increment()
    {
        lock (_lock) // Only one thread can execute this block at a time
        {
            _count++;
        }
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _count;
            }
        }
    }
}
```

## 6. Joining and Interrupting Threads

Joining threads:

```csharp
Thread thread = new Thread(DoWork);
thread.Start();
// Wait for the thread to complete before continuing
thread.Join(); // Blocks until thread completes
// or
thread.Join(1000); // Wait up to 1 second for thread to complete
```

Interrupting threads:

```csharp
Thread thread = new Thread(() =>
{
    try
    {
        while (true)
        {
            // Check if thread has been interrupted
            Thread.Sleep(100); // This will throw when interrupted
        }
    }
    catch (ThreadInterruptedException)
    {
        Console.WriteLine("Thread was interrupted");
    }
});
thread.Start();
Thread.Sleep(1000);
thread.Interrupt(); // Signal the thread to stop
```

## 7. ThreadPool Basics

The ThreadPool manages a pool of worker threads to reduce the overhead of thread creation:

```csharp
static void Main()
{
    // Queue work item to the thread pool
    ThreadPool.QueueUserWorkItem(state => 
    {
        Console.WriteLine("Work item executed on thread pool");
    });
    
    // Get thread pool information
    ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
    Console.WriteLine($"Max worker threads: {workerThreads}");
    Console.WriteLine($"Max completion port threads: {completionPortThreads}");
    
    Thread.Sleep(2000); // Wait for thread pool work to complete
}
```

## 8. Avoiding Race Conditions

Race conditions occur when multiple threads access shared data with unexpected timing. Here's an example and how to fix it:

```csharp
// Unsafe example (race condition)
class UnsafeCounter
{
    private int _count = 0;
    
    public void Increment()
    {
        _count = _count + 1; // This operation is not atomic!
    }
    
    public int Count => _count;
}
// Safe example using Interlocked
class SafeCounter
{
    private int _count = 0;
    
    public void Increment()
    {
        Interlocked.Increment(ref _count); // Atomic operation
    }
    
    public int Count => Interlocked.CompareExchange(ref _count, 0, 0);
}
```

## 9. Thread-local Storage

For data that should be unique to each thread:

```csharp
// Static thread-local field
private static ThreadLocal<Random> _random = 
    new ThreadLocal<Random>(() => new Random(Thread.CurrentThread.ManagedThreadId));
// Usage
public int GetRandomNumber()
{
    return _random.Value.Next(100);
}
```

## Practical Example: Simple Producer-Consumer

Let's put these concepts together with a basic producer-consumer pattern that might be used in robotic applications:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
class Program
{
    static Queue<int> _queue = new Queue<int>();
    static object _lock = new object();
    static bool _done = false;
    
    static void Main()
    {
        // Create producer and consumer threads
        Thread producerThread = new Thread(Producer);
        Thread consumerThread = new Thread(Consumer);
        
        producerThread.Start();
        consumerThread.Start();
        
        // Let it run for a while
        Thread.Sleep(5000);
        
        // Signal completion
        lock (_lock)
        {
            _done = true;
            // Wake up consumer if it's waiting
            Monitor.PulseAll(_lock);
        }
        
        // Wait for threads to finish
        producerThread.Join();
        consumerThread.Join();
    }
    
    static void Producer()
    {
        Random random = new Random();
        
        while (!_done)
        {
            // Generate a value (e.g., sensor reading)
            int value = random.Next(100);
            
            lock (_lock)
            {
                // Add to queue
                _queue.Enqueue(value);
                Console.WriteLine($"Produced: {value}, Queue size: {_queue.Count}");
                
                // Notify consumer that data is available
                Monitor.Pulse(_lock);
            }
            
            // Simulate some work
            Thread.Sleep(random.Next(500, 1000));
        }
    }
    
    static void Consumer()
    {
        while (true)
        {
            int value;
            
            lock (_lock)
            {
                // If queue is empty and we're not done, wait
                while (_queue.Count == 0)
                {
                    if (_done)
                        return; // Exit if we're done and queue is empty
                        
                    // Wait for producer to add something
                    Monitor.Wait(_lock);
                }
                
                // Get item from queue
                value = _queue.Dequeue();
            }
            
            // Process the value (e.g., control motor)
            Console.WriteLine($"Consumed: {value}");
            
            // Simulate processing time
            Thread.Sleep(200);
        }
    }
}
```

This example demonstrates:
- Thread creation and starting
- Thread synchronization with lock
- Thread coordination with Monitor.Wait and Monitor.Pulse
- Safely sharing data between threads
- Proper thread shutdown