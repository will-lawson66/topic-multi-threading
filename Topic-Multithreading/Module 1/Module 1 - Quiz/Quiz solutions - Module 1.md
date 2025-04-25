# Module 1: Thread Fundamentals - Quiz Solutions

## Theory Questions

### 1. What namespace contains the Thread class in C#?
**Answer**: `System.Threading`

### 2. Describe the difference between foreground and background threads.
**Answer**: 
- **Foreground threads** keep the application running until they complete, even if the main thread finishes. 
- **Background threads** (set with `Thread.IsBackground = true`) automatically terminate when all foreground threads have exited. They're useful for tasks that can be safely terminated when the application exits.

### 3. List and describe at least three possible thread states.
**Answer**:
- **Unstarted**: Thread is created but `Start()` has not been called
- **Running**: Thread is executing code
- **WaitSleepJoin**: Thread is blocked (sleeping, waiting, or joined to another thread)
- **Stopped**: Thread has completed execution
- **Suspended**: Thread execution is temporarily halted (deprecated)

### 4. What is a race condition and why is it problematic?
**Answer**: A race condition occurs when multiple threads access and modify shared data concurrently, and the outcome depends on the precise timing/sequence of operations. Race conditions are problematic because they cause unpredictable behavior, intermittent bugs that are difficult to reproduce, and data corruption.

### 5. What is the difference between using ThreadStart and ParameterizedThreadStart?
**Answer**: `ThreadStart` is used when creating a thread that doesn't require parameters. `ParameterizedThreadStart` allows passing a single object parameter to the thread method, enabling data to be passed to the thread at startup.

### 6. What is thread affinity and how does it impact performance?
**Answer**: Thread affinity refers to the association between a thread and a specific CPU core. High thread affinity means a thread tends to run on the same core, which can improve cache utilization. However, enforcing strict thread affinity can limit the scheduler's ability to balance workloads across all available cores, potentially reducing overall performance.

## Code Exercises

### Exercise 1: Create a simple thread
Write code to create and start a thread that prints "Hello from the worker thread!" to the console.

**Solution**:
```csharp
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        Thread workerThread = new Thread(WorkerMethod);
        workerThread.Start();
        
        Console.WriteLine("Hello from the main thread!");
    }
    
    static void WorkerMethod()
    {
        Console.WriteLine("Hello from the worker thread!");
    }
}
```

### Exercise 2: Thread with parameters
Create a thread that accepts a string parameter and prints it to the console.

**Solution**:
```csharp
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        Thread workerThread = new Thread(PrintMessage);
        workerThread.Start("This is a message from the main thread");
        
        Console.WriteLine("Main thread continues execution...");
    }
    
    static void PrintMessage(object message)
    {
        string messageStr = message as string;
        Console.WriteLine($"Worker thread received: {messageStr}");
    }
}
```

### Exercise 3: Fix a race condition
The following code has a race condition. Fix it using proper synchronization.

**Original code with race condition**:
```csharp
class Counter
{
    private int _count = 0;
    
    public void Increment()
    {
        _count = _count + 1;
    }
    
    public int Count
    {
        get { return _count; }
    }
}
```

**Solution using lock**:
```csharp
class Counter
{
    private int _count = 0;
    private readonly object _lock = new object();
    
    public void Increment()
    {
        lock (_lock)
        {
            _count = _count + 1;
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

**Alternative solution using Interlocked**:
```csharp
class AtomicCounter
{
    private int _count = 0;
    
    public void Increment()
    {
        Interlocked.Increment(ref _count);
    }
    
    public int Count
    {
        get 
        { 
            return Interlocked.CompareExchange(ref _count, 0, 0);
        }
    }
}
```

### Exercise 4: Thread joining
Write code that creates two threads, waits for both to complete, and then prints "All threads completed" to the console.

**Solution**:
```csharp
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        Thread thread1 = new Thread(() => {
            Console.WriteLine("Thread 1 starting...");
            Thread.Sleep(2000);
            Console.WriteLine("Thread 1 completed.");
        });
        
        Thread thread2 = new Thread(() => {
            Console.WriteLine("Thread 2 starting...");
            Thread.Sleep(1000);
            Console.WriteLine("Thread 2 completed.");
        });
        
        thread1.Start();
        thread2.Start();
        
        // Wait for both threads to complete
        thread1.Join();
        thread2.Join();
        
        Console.WriteLine("All threads completed.");
    }
}
```

### Exercise 5: Implementing a simple producer-consumer pattern
Create a thread-safe queue where one thread adds random numbers and another thread processes them. Use a lock for synchronization.

**Solution**:
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
        Thread producerThread = new Thread(Producer);
        Thread consumerThread = new Thread(Consumer);
        
        producerThread.Start();
        consumerThread.Start();
        
        // Let the program run for 5 seconds
        Thread.Sleep(5000);
        
        // Signal threads to complete
        lock (_lock)
        {
            _done = true;
            Monitor.PulseAll(_lock);
        }
        
        // Wait for both threads to complete
        producerThread.Join();
        consumerThread.Join();
        
        Console.WriteLine("Program completed.");
    }
    
    static void Producer()
    {
        Random random = new Random();
        
        while (!_done)
        {
            int value = random.Next(100);
            
            lock (_lock)
            {
                _queue.Enqueue(value);
                Console.WriteLine($"Produced: {value}");
                
                // Signal that data is available
                Monitor.Pulse(_lock);
            }
            
            Thread.Sleep(500);
        }
        
        Console.WriteLine("Producer completed.");
    }
    
    static void Consumer()
    {
        while (true)
        {
            int value;
            bool hasValue = false;
            
            lock (_lock)
            {
                while (_queue.Count == 0)
                {
                    if (_done)
                    {
                        Console.WriteLine("Consumer completed.");
                        return;
                    }
                    
                    // Wait for data or completion signal
                    Monitor.Wait(_lock);
                }
                
                value = _queue.Dequeue();
                hasValue = true;
            }
            
            if (hasValue)
            {
                Console.WriteLine($"Consumed: {value}");
                Thread.Sleep(200);
            }
        }
    }
}
```