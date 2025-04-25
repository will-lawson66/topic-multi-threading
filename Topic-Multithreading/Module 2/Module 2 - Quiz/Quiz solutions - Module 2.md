# Module 2: Synchronization Mechanisms - Quiz Solutions

## Theory Questions

### 1. What is the difference between the lock statement and directly using the Monitor class?
**Answer**: The `lock` statement is syntactic sugar for using the `Monitor` class. It automatically handles the try/finally block to ensure `Monitor.Exit()` is called even if exceptions occur. The `Monitor` class provides additional functionality not available with `lock`, such as timeouts with `TryEnter()` and signaling with `Wait()`, `Pulse()`, and `PulseAll()`.

### 2. Explain the difference between ManualResetEvent and AutoResetEvent.
**Answer**: Both are synchronization primitives that signal threads, but they differ in how they reset:
- **ManualResetEvent**: When signaled (`Set`), it remains in that state until explicitly `Reset`, allowing all waiting threads to proceed.
- **AutoResetEvent**: When signaled, it releases only one waiting thread and then automatically returns to the unsignaled state.

### 3. When would you use Interlocked operations instead of a lock statement?
**Answer**: Use Interlocked operations when:
- You need to perform simple atomic operations on numeric values (increment, decrement, exchange)
- You want better performance, as Interlocked has less overhead than lock
- You need to avoid potential deadlocks
- You're updating a single variable and don't need to protect a larger code block

### 4. What is a deadlock and how can it be prevented?
**Answer**: A deadlock occurs when two or more threads wait indefinitely for each other to release locks, causing the application to hang. Prevention strategies include:
- Consistent lock ordering (always acquire locks in the same order)
- Using timeouts with `Monitor.TryEnter()` instead of indefinite waiting
- Avoiding nested locks when possible
- Using lock hierarchies
- Using higher-level synchronization primitives
- Designing to minimize shared resources

### 5. What is the purpose of ReaderWriterLockSlim and when would you use it?
**Answer**: `ReaderWriterLockSlim` allows multiple threads to read a resource simultaneously while ensuring exclusive access for writing. It's optimized for scenarios where reads are frequent but writes are rare. Use it when:
- Your data structure is read frequently but updated infrequently
- You want to maximize concurrency for read operations
- You need better performance than using a simple lock for all operations

### 6. Explain the purpose of the Barrier class and provide an example scenario where it would be useful.
**Answer**: The `Barrier` class synchronizes multiple threads at specific points in an algorithm, ensuring all threads reach a certain point before any proceed. It's useful for:
- Phased algorithms where each phase depends on all threads completing the previous phase
- Parallel computations requiring synchronization at specific points
- Example: In a robotic system where multiple sensor-processing threads must complete their analysis before the next control decision can be made

## Code Exercises

### Exercise 1: Fix a potential deadlock
The following code can cause a deadlock. Identify and fix the issue.

**Original code with deadlock issue**:
```csharp
class DeadlockExample
{
    private readonly object _lockA = new object();
    private readonly object _lockB = new object();
    
    public void MethodA()
    {
        lock (_lockA)
        {
            Thread.Sleep(1000); // Simulate work
            lock (_lockB)
            {
                Console.WriteLine("MethodA completed");
            }
        }
    }
    
    public void MethodB()
    {
        lock (_lockB)
        {
            Thread.Sleep(1000); // Simulate work
            lock (_lockA)
            {
                Console.WriteLine("MethodB completed");
            }
        }
    }
}
```

**Solution**:
```csharp
class DeadlockFixedExample
{
    private readonly object _lockA = new object();
    private readonly object _lockB = new object();
    
    public void MethodA()
    {
        lock (_lockA)
        {
            Thread.Sleep(1000); // Simulate work
            lock (_lockB)
            {
                Console.WriteLine("MethodA completed");
            }
        }
    }
    
    public void MethodB()
    {
        lock (_lockA)  // Fix: acquire locks in the same order as MethodA
        {
            Thread.Sleep(1000); // Simulate work
            lock (_lockB)
            {
                Console.WriteLine("MethodB completed");
            }
        }
    }
}
```

### Exercise 2: Using ManualResetEvent for thread coordination
Write code that creates three worker threads that each perform a task, and then uses a ManualResetEvent to signal all threads to start simultaneously.

**Solution**:
```csharp
using System;
using System.Threading;

class Program
{
    static ManualResetEvent _startEvent = new ManualResetEvent(false);
    
    static void Main()
    {
        Console.WriteLine("Creating threads...");
        
        Thread[] threads = new Thread[3];
        for (int i = 0; i < threads.Length; i++)
        {
            int threadNum = i;
            threads[i] = new Thread(() => DoWork(threadNum));
            threads[i].Start();
        }
        
        // Give threads time to initialize
        Thread.Sleep(1000);
        Console.WriteLine("All threads ready. Starting work...");
        
        // Signal all threads to start
        _startEvent.Set();
        
        // Wait for all threads to complete
        foreach (var thread in threads)
        {
            thread.Join();
        }
        
        Console.WriteLine("All work completed.");
    }
    
    static void DoWork(int threadNumber)
    {
        Console.WriteLine($"Thread {threadNumber} waiting to start...");
        
        // Wait for the signal to start
        _startEvent.WaitOne();
        
        Console.WriteLine($"Thread {threadNumber} starting work...");
        
        // Simulate work
        Thread.Sleep(1000 + threadNumber * 500);
        
        Console.WriteLine($"Thread {threadNumber} completed work.");
    }
}
```

### Exercise 3: Implement a thread-safe singleton pattern
Create a thread-safe singleton class using the double-check locking pattern.

**Solution**:
```csharp
public sealed class Singleton
{
    private static volatile Singleton _instance;
    private static readonly object _lock = new object();
    
    // Private constructor to prevent direct instantiation
    private Singleton() { }
    
    public static Singleton Instance
    {
        get
        {
            // First check without locking
            if (_instance == null)
            {
                lock (_lock)
                {
                    // Second check with locking
                    if (_instance == null)
                    {
                        _instance = new Singleton();
                    }
                }
            }
            
            return _instance;
        }
    }
    
    // Example method
    public void DoSomething()
    {
        Console.WriteLine("Singleton is doing something");
    }
}
```

### Exercise 4: Implement a bounded buffer using Monitor
Create a bounded buffer that can hold a maximum number of items, using Monitor's Wait() and Pulse() methods for synchronization.

**Solution**:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;

class BoundedBuffer<T>
{
    private readonly Queue<T> _queue = new Queue<T>();
    private readonly int _capacity;
    private readonly object _lock = new object();
    
    public BoundedBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
            
        _capacity = capacity;
    }
    
    public void Enqueue(T item)
    {
        lock (_lock)
        {
            // Wait while the buffer is full
            while (_queue.Count >= _capacity)
            {
                Monitor.Wait(_lock);
            }
            
            _queue.Enqueue(item);
            
            // Signal that an item is available for dequeuing
            Monitor.Pulse(_lock);
        }
    }
    
    public T Dequeue()
    {
        lock (_lock)
        {
            // Wait while the buffer is empty
            while (_queue.Count == 0)
            {
                Monitor.Wait(_lock);
            }
            
            T item = _queue.Dequeue();
            
            // Signal that space is available for enqueuing
            Monitor.Pulse(_lock);
            
            return item;
        }
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _queue.Count;
            }
        }
    }
}
```

### Exercise 5: Using ReaderWriterLockSlim
Implement a thread-safe cache that uses ReaderWriterLockSlim to allow concurrent reads but exclusive writes.

**Solution**:
```csharp
using System;
using System.Collections.Generic;
using System.Threading;

class ThreadSafeCache<TKey, TValue>
{
    private Dictionary<TKey, TValue> _cache = new Dictionary<TKey, TValue>();
    private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        // First try to get value with a reader lock
        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out TValue value))
            {
                return value;
            }
        }
        finally
        {
            _lock.ExitReadLock();
        }
        
        // If not found, upgrade to a writer lock
        _lock.EnterWriteLock();
        try
        {
            // Check again in case another thread added it
            if (_cache.TryGetValue(key, out TValue value))
            {
                return value;
            }
            
            // Actually add the new item
            value = valueFactory(key);
            _cache.Add(key, value);
            return value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public bool TryGetValue(TKey key, out TValue value)
    {
        _lock.EnterReadLock();
        try
        {
            return _cache.TryGetValue(key, out value);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public void AddOrUpdate(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            _cache[key] = value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public bool Remove(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            return _cache.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}
```