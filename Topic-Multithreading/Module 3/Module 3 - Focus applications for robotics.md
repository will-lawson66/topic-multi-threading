# Module 3: Focus Applications for Robotics

In this module, we'll explore practical threading patterns specifically designed for robotics applications, focusing on producer-consumer patterns and channel implementations. These patterns form the foundation of many robotic systems where real-time data processing and component coordination are critical.

## Producer-Consumer Pattern

The producer-consumer pattern is one of the most commonly used patterns in multithreaded robotics applications. It's ideal for decoupling data production (sensors, control inputs) from consumption (processing, actuation).

### Basic Implementation with a Shared Queue

Here's a basic producer-consumer implementation using a thread-safe queue:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class ProducerConsumer
{
    private readonly Queue<SensorData> _queue = new Queue<SensorData>();
    private readonly object _queueLock = new object();
    private readonly int _maxQueueSize;
    private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
    private readonly AutoResetEvent _dataAvailableEvent = new AutoResetEvent(false);
    private readonly AutoResetEvent _spaceAvailableEvent = new AutoResetEvent(true);
    
    private Thread _producerThread;
    private Thread _consumerThread;
    
    public ProducerConsumer(int maxQueueSize = 100)
    {
        _maxQueueSize = maxQueueSize;
    }
    
    public void Start()
    {
        _producerThread = new Thread(ProducerWork)
        {
            Name = "ProducerThread",
            IsBackground = true
        };
        
        _consumerThread = new Thread(ConsumerWork)
        {
            Name = "ConsumerThread",
            IsBackground = true
        };
        
        _producerThread.Start();
        _consumerThread.Start();
    }
    
    public void Stop()
    {
        _stopEvent.Set();
        _dataAvailableEvent.Set(); // Ensure consumer isn't blocked
        _spaceAvailableEvent.Set(); // Ensure producer isn't blocked
        
        _producerThread.Join(1000);
        _consumerThread.Join(1000);
    }
    
    private void ProducerWork()
    {
        Random random = new Random();
        
        while (!_stopEvent.WaitOne(0))
        {
            // Wait until there's space in the queue
            if (!_spaceAvailableEvent.WaitOne(100))
                continue;
                
            // Simulate getting data from sensor
            SensorData data = new SensorData
            {
                Value = random.NextDouble() * 100,
                Timestamp = DateTime.Now
            };
            
            lock (_queueLock)
            {
                _queue.Enqueue(data);
                Console.WriteLine($"Produced: {data.Value:F2} at {data.Timestamp}");
                
                // Signal consumer that data is available
                _dataAvailableEvent.Set();
                
                // If queue is full, reset the space available event
                if (_queue.Count >= _maxQueueSize)
                    _spaceAvailableEvent.Reset();
            }
            
            // Simulate sensor reading frequency
            Thread.Sleep(200);
        }
    }
    
    private void ConsumerWork()
    {
        while (!_stopEvent.WaitOne(0))
        {
            // Wait until data is available
            if (!_dataAvailableEvent.WaitOne(100))
                continue;
                
            SensorData data = null;
            bool hasData = false;
            
            lock (_queueLock)
            {
                if (_queue.Count > 0)
                {
                    data = _queue.Dequeue();
                    hasData = true;
                    
                    // If queue was full but now has space, signal producer
                    if (_queue.Count < _maxQueueSize)
                        _spaceAvailableEvent.Set();
                        
                    // If queue is empty, reset the data available event
                    if (_queue.Count == 0)
                        _dataAvailableEvent.Reset();
                }
            }
            
            if (hasData)
            {
                // Process the data
                Console.WriteLine($"Consumed: {data.Value:F2}, Age: {(DateTime.Now - data.Timestamp).TotalMilliseconds:F0}ms");
                
                // Simulate processing time
                Thread.Sleep(300);
            }
        }
    }
    
    public class SensorData
    {
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

### Using BlockingCollection for Simpler Implementation

The BlockingCollection<T> class greatly simplifies producer-consumer implementations:

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;

public class BlockingCollectionProducerConsumer
{
    private readonly BlockingCollection<SensorData> _dataQueue;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Thread _producerThread;
    private readonly Thread _consumerThread;
    
    public BlockingCollectionProducerConsumer(int capacity = 100)
    {
        // Create a bounded BlockingCollection
        _dataQueue = new BlockingCollection<SensorData>(capacity);
        
        _producerThread = new Thread(ProducerWork)
        {
            Name = "Producer",
            IsBackground = true
        };
        
        _consumerThread = new Thread(ConsumerWork)
        {
            Name = "Consumer",
            IsBackground = true
        };
    }
    
    public void Start()
    {
        _producerThread.Start();
        _consumerThread.Start();
    }
    
    public void Stop()
    {
        _cts.Cancel();
        _dataQueue.CompleteAdding();
        
        _producerThread.Join(1000);
        _consumerThread.Join(1000);
    }
    
    private void ProducerWork()
    {
        Random random = new Random();
        
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var data = new SensorData
                {
                    Value = random.NextDouble() * 100,
                    Timestamp = DateTime.Now
                };
                
                // Will automatically block if collection is at capacity
                _dataQueue.Add(data, _cts.Token);
                
                Console.WriteLine($"Produced: {data.Value:F2}");
                Thread.Sleep(100);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Producer canceled");
        }
    }
    
    private void ConsumerWork()
    {
        try
        {
            // GetConsumingEnumerable blocks if no items are available
            // and completes when CompleteAdding is called
            foreach (var data in _dataQueue.GetConsumingEnumerable(_cts.Token))
            {
                Console.WriteLine($"Consumed: {data.Value:F2}, Age: {(DateTime.Now - data.Timestamp).TotalMilliseconds:F0}ms");
                Thread.Sleep(200);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer canceled");
        }
    }
    
    public class SensorData
    {
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

## Channels Implementation

While .NET doesn't have a built-in Channel type until recent versions, we can implement our own channel mechanism that's purpose-built for robotics applications.

### Custom Bounded Channel Implementation

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class RoboticsChannel<T>
{
    private readonly Queue<T> _queue = new Queue<T>();
    private readonly int _capacity;
    private readonly object _syncLock = new object();
    private bool _isCompleted;
    
    public RoboticsChannel(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        
        _capacity = capacity;
    }
    
    public bool TryWrite(T item, int timeoutMs = Timeout.Infinite)
    {
        lock (_syncLock)
        {
            if (_isCompleted)
                return false;
            
            // Wait until there's space or timeout
            var startTime = Environment.TickCount;
            while (_queue.Count >= _capacity)
            {
                if (timeoutMs == 0)
                    return false;
                
                var remainingTime = timeoutMs == Timeout.Infinite
                    ? Timeout.Infinite
                    : timeoutMs - (Environment.TickCount - startTime);
                
                if (remainingTime <= 0)
                    return false;
                
                // Wait for space to become available
                if (!Monitor.Wait(_syncLock, remainingTime))
                    return false;
                
                if (_isCompleted)
                    return false;
            }
            
            _queue.Enqueue(item);
            
            // Signal that data is available
            Monitor.PulseAll(_syncLock);
            return true;
        }
    }
    
    public bool TryRead(out T item, int timeoutMs = Timeout.Infinite)
    {
        lock (_syncLock)
        {
            // Wait until there's data or timeout
            var startTime = Environment.TickCount;
            while (_queue.Count == 0)
            {
                if (_isCompleted)
                {
                    item = default;
                    return false;
                }
                
                if (timeoutMs == 0)
                {
                    item = default;
                    return false;
                }
                
                var remainingTime = timeoutMs == Timeout.Infinite
                    ? Timeout.Infinite
                    : timeoutMs - (Environment.TickCount - startTime);
                
                if (remainingTime <= 0)
                {
                    item = default;
                    return false;
                }
                
                // Wait for data to become available
                if (!Monitor.Wait(_syncLock, remainingTime))
                {
                    item = default;
                    return false;
                }
                
                if (_queue.Count == 0 && _isCompleted)
                {
                    item = default;
                    return false;
                }
            }
            
            item = _queue.Dequeue();
            
            // Signal that space is available
            Monitor.PulseAll(_syncLock);
            return true;
        }
    }
    
    public void Complete()
    {
        lock (_syncLock)
        {
            _isCompleted = true;
            Monitor.PulseAll(_syncLock);
        }
    }
    
    public int Count
    {
        get
        {
            lock (_syncLock)
            {
                return _queue.Count;
            }
        }
    }
}
```

### Priority Channel for Critical Robotics Operations

In robotic systems, some messages need higher priority. Here's a priority channel implementation:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class PriorityRoboticsChannel<T>
{
    // Higher numbers have higher priority (descending order)
    private readonly SortedList<int, Queue<T>> _priorityQueues = 
        new SortedList<int, Queue<T>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
    
    private readonly int _capacity;
    private readonly object _syncLock = new object();
    private bool _isCompleted;
    private int _totalCount = 0;
    
    public PriorityRoboticsChannel(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        
        _capacity = capacity;
    }
    
    public bool TryWrite(T item, int priority, int timeoutMs = Timeout.Infinite)
    {
        lock (_syncLock)
        {
            if (_isCompleted)
                return false;
            
            // Wait until there's space or timeout
            var startTime = Environment.TickCount;
            while (_totalCount >= _capacity)
            {
                if (timeoutMs == 0)
                    return false;
                
                var remainingTime = timeoutMs == Timeout.Infinite
                    ? Timeout.Infinite
                    : timeoutMs - (Environment.TickCount - startTime);
                
                if (remainingTime <= 0)
                    return false;
                
                // Wait for space to become available
                if (!Monitor.Wait(_syncLock, remainingTime))
                    return false;
                
                if (_isCompleted)
                    return false;
            }
            
            // Get or create queue for this priority level
            if (!_priorityQueues.TryGetValue(priority, out Queue<T> queue))
            {
                queue = new Queue<T>();
                _priorityQueues.Add(priority, queue);
            }
            
            queue.Enqueue(item);
            _totalCount++;
            
            // Signal that data is available
            Monitor.PulseAll(_syncLock);
            return true;
        }
    }
    
    public bool TryRead(out T item, out int priority, int timeoutMs = Timeout.Infinite)
    {
        lock (_syncLock)
        {
            // Wait until there's data or timeout
            var startTime = Environment.TickCount;
            while (_totalCount == 0)
            {
                if (_isCompleted)
                {
                    item = default;
                    priority = 0;
                    return false;
                }
                
                if (timeoutMs == 0)
                {
                    item = default;
                    priority = 0;
                    return false;
                }
                
                var remainingTime = timeoutMs == Timeout.Infinite
                    ? Timeout.Infinite
                    : timeoutMs - (Environment.TickCount - startTime);
                
                if (remainingTime <= 0)
                {
                    item = default;
                    priority = 0;
                    return false;
                }
                
                // Wait for data to become available
                if (!Monitor.Wait(_syncLock, remainingTime))
                {
                    item = default;
                    priority = 0;
                    return false;
                }
                
                if (_totalCount == 0 && _isCompleted)
                {
                    item = default;
                    priority = 0;
                    return false;
                }
            }
            
            // Get highest priority queue (already sorted by the SortedList)
            var highestPriority = _priorityQueues.Keys[0];
            var highestPriorityQueue = _priorityQueues[highestPriority];
            
            item = highestPriorityQueue.Dequeue();
            priority = highestPriority;
            _totalCount--;
            
            // Remove queue if empty
            if (highestPriorityQueue.Count == 0)
                _priorityQueues.Remove(highestPriority);
            
            // Signal that space is available
            Monitor.PulseAll(_syncLock);
            return true;
        }
    }
    
    public void Complete()
    {
        lock (_syncLock)
        {
            _isCompleted = true;
            Monitor.PulseAll(_syncLock);
        }
    }
    
    public int Count
    {
        get
        {
            lock (_syncLock)
            {
                return _totalCount;
            }
        }
    }
}
```

## Event-Based Notification System

For certain robotics scenarios, an event-based system is more appropriate than direct message passing:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

// Event types for robot sensors
public enum SensorEventType
{
    Temperature,
    Proximity,
    Motion,
    Position,
    Battery
}

// Event data class
public class SensorEvent
{
    public SensorEventType Type { get; }
    public int SensorId { get; }
    public double Value { get; }
    public DateTime Timestamp { get; }
    
    public SensorEvent(SensorEventType type, int sensorId, double value)
    {
        Type = type;
        SensorId = sensorId;
        Value = value;
        Timestamp = DateTime.Now;
    }
    
    public override string ToString()
    {
        return $"Sensor {SensorId} ({Type}): {Value:F2} at {Timestamp:HH:mm:ss.fff}";
    }
}

// Delegate for event handlers
public delegate void SensorEventHandler(SensorEvent sensorEvent);

// Event broker that manages subscriptions and dispatches events
public class SensorEventBroker
{
    private readonly Dictionary<SensorEventType, List<SensorEventHandler>> _subscribers = 
        new Dictionary<SensorEventType, List<SensorEventHandler>>();
    
    private readonly object _syncLock = new object();
    
    // Subscribe to events of a specific type
    public void Subscribe(SensorEventType eventType, SensorEventHandler handler)
    {
        lock (_syncLock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<SensorEventHandler>();
                _subscribers[eventType] = handlers;
            }
            
            handlers.Add(handler);
        }
    }
    
    // Unsubscribe from events of a specific type
    public void Unsubscribe(SensorEventType eventType, SensorEventHandler handler)
    {
        lock (_syncLock)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                
                if (handlers.Count == 0)
                {
                    _subscribers.Remove(eventType);
                }
            }
        }
    }
    
    // Publish an event to all subscribers
    public void Publish(SensorEvent sensorEvent)
    {
        List<SensorEventHandler> handlersToNotify = null;
        
        lock (_syncLock)
        {
            if (_subscribers.TryGetValue(sensorEvent.Type, out var handlers))
            {
                // Create a copy to avoid issues if handlers subscribe/unsubscribe during notification
                handlersToNotify = new List<SensorEventHandler>(handlers);
            }
        }
        
        // Notify subscribers outside the lock
        if (handlersToNotify != null)
        {
            foreach (var handler in handlersToNotify)
            {
                try
                {
                    handler(sensorEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in event handler: {ex.Message}");
                    // In a real system, you'd want better error handling here
                }
            }
        }
    }
}
```

## Summary

These patterns provide the foundation for building robust multi-threaded robotics applications:

1. **Producer-Consumer Pattern**: Ideal for sensor data processing, command queuing, and decoupled communication between subsystems

2. **Custom Channels**: Provide a higher-level abstraction for message passing with additional features like bounded capacity, timeouts, and priorities

3. **Event-Based Systems**: Great for loosely coupled components that need to react to various events in the robot system