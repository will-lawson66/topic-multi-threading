# Module 3: Focus Applications for Robotics Quiz Solutions

## Theory Questions

### 1. Explain the producer-consumer pattern and why it's useful in robotics applications.

**Answer:** The producer-consumer pattern is a concurrency design pattern where "producer" threads create data (e.g., sensor readings, commands) and add them to a shared buffer, while "consumer" threads take and process that data. It's useful in robotics applications because:

- It decouples data acquisition from processing
- It handles different production and consumption rates
- It facilitates parallel processing of sensor data
- It creates clean boundaries between subsystems (sensors, processing, actuation)
- It improves responsiveness by allowing faster sensors to run independently of slower processing

### 2. What is a channel in the context of multithreaded applications, and how does it differ from a simple shared queue?

**Answer:** A channel is a higher-level abstraction for passing data between threads, similar to a pipe. Unlike a simple shared queue, channels typically:

- Provide built-in synchronization
- Support bounded capacity with back-pressure
- Offer completion signaling
- Can have multiple producers and consumers
- Often provide clean APIs for async operations
- May include mechanisms for prioritization or filtering
- Handle concurrency details internally, leading to safer code

### 3. What is the difference between bounded and unbounded channels?

**Answer:**

- **Bounded channels**: Have a fixed maximum capacity. When full, producers either block or get notified that the operation couldn't complete. They provide back-pressure, prevent memory exhaustion, and are suitable for scenarios where consumers might be slower than producers.
- **Unbounded channels**: Have no predefined capacity limit (except for system memory). They never block producers but risk memory issues if producers outpace consumers. They're suitable when production rate is naturally limited or unpredictable bursts need to be handled without blocking.

### 4. How would you implement priority handling in a producer-consumer system for a robot?

**Answer:** Priority handling in a producer-consumer system for a robot can be implemented by:

- Using multiple queues for different priority levels
- Implementing a priority queue data structure with thread-safe access
- Adding priority information to message objects and sorting in the consumer
- Using a custom channel implementation that respects priority
- Defining separate threads for high-priority vs. low-priority tasks
- Implementing a dispatcher that checks high-priority queues first
- Using thread priorities to ensure critical processors get more CPU time

### 5. Explain the concept of back-pressure in a channel-based system and why it's important.

**Answer:** Back-pressure is a mechanism where a system slows down producers when consumers can't keep up with the data rate. It's important because:

- It prevents memory exhaustion from unbounded queuing
- It helps maintain system stability under load
- It provides natural flow control
- It makes overload conditions visible rather than hiding them
- It forces system designers to consider data flow rates
- It prevents lower-priority data from overwhelming critical processing
- In robotics, it ensures critical control loops remain responsive

### 6. What are the trade-offs between using BlockingCollection<T> versus implementing a custom channel?

**Answer:** 

**Using BlockingCollection<T>**
- **Pros**: Built-in, well-tested, handles concurrency, simple API, supports timeouts
- **Cons**: Limited customization, no direct priority support, less control over internals

**Custom Channel Implementation**
- **Pros**: Customizable behavior, can optimize for specific patterns, can add priorities, special handling for robotics-specific needs
- **Cons**: More complex to implement correctly, requires thorough testing, potential for bugs in concurrency handling

## Code Exercises

### Exercise 1: Implement a basic producer-consumer pattern using BlockingCollection

Create a producer-consumer system that simulates a robot processing sensor data. The producer generates random sensor readings, and the consumer processes them.

**Solution:**

```csharp
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

class SensorDataProcessor
{
    private readonly BlockingCollection<SensorReading> _dataQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Thread _producerThread;
    private readonly Thread _consumerThread;
    
    public SensorDataProcessor(int queueCapacity = 100)
    {
        _dataQueue = new BlockingCollection<SensorReading>(queueCapacity);
        _cts = new CancellationTokenSource();
        
        _producerThread = new Thread(ProducerWork)
        {
            Name = "SensorProducer",
            IsBackground = true
        };
        
        _consumerThread = new Thread(ConsumerWork)
        {
            Name = "SensorConsumer",
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
        var random = new Random();
        
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                // Simulate sensor reading
                var reading = new SensorReading
                {
                    SensorId = random.Next(1, 5),
                    Value = random.NextDouble() * 100,
                    Timestamp = DateTime.Now
                };
                
                _dataQueue.Add(reading);
                Console.WriteLine($"Produced: Sensor {reading.SensorId}, Value: {reading.Value:F2}");
                
                // Simulate sensor update frequency
                Thread.Sleep(200);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Producer cancelled");
        }
        finally
        {
            _dataQueue.CompleteAdding();
        }
    }
    
    private void ConsumerWork()
    {
        try
        {
            foreach (var reading in _dataQueue.GetConsumingEnumerable(_cts.Token))
            {
                // Process the sensor reading
                Console.WriteLine($"Processing: Sensor {reading.SensorId}, Value: {reading.Value:F2}, Age: {(DateTime.Now - reading.Timestamp).TotalMilliseconds:F0}ms");
                
                // Simulate processing time
                Thread.Sleep(300);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer cancelled");
        }
    }
}

class SensorReading
{
    public int SensorId { get; set; }
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

class Program
{
    static void Main()
    {
        var processor = new SensorDataProcessor();
        processor.Start();
        
        Console.WriteLine("Processing sensor data. Press Enter to stop...");
        Console.ReadLine();
        
        processor.Stop();
        Console.WriteLine("Processing stopped.");
    }
}
```

### Exercise 2: Implement a custom bounded channel

Create a custom channel implementation for message passing that has a bounded capacity and blocks producers when full.

**Solution:**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class BoundedChannel<T>
{
    private readonly Queue<T> _queue = new Queue<T>();
    private readonly int _capacity;
    private readonly object _syncLock = new object();
    private bool _isCompleted;
    
    public BoundedChannel(int capacity)
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
            Monitor.Pulse(_syncLock);
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
            Monitor.Pulse(_syncLock);
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

### Exercise 3: Implement a priority channel

Extend the bounded channel implementation to support message priorities, where higher priority messages are processed before lower priority ones.

**Solution:**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class PriorityChannel<T>
{
    // Higher numbers have higher priority (descending order)
    private readonly SortedList<int, Queue<T>> _priorityQueues = 
        new SortedList<int, Queue<T>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
    
    private readonly int _capacity;
    private readonly object _syncLock = new object();
    private bool _isCompleted;
    private int _totalCount = 0;
    
    public PriorityChannel(int capacity)
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

### Exercise 4: Implement an event-based notification system

Create a system where multiple sensors can publish events, and multiple subscribers can receive notifications based on the type of event they're interested in.

**Solution:**

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
        return $"[{Timestamp:HH:mm:ss.fff}] Sensor {SensorId} ({Type}): {Value:F2}";
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

// Example sensor device that publishes events
public class Sensor
{
    private readonly SensorEventBroker _eventBroker;
    private readonly int _sensorId;
    private readonly SensorEventType _sensorType;
    private readonly Thread _workerThread;
    private readonly Random _random = new Random();
    private volatile bool _isRunning;
    
    public Sensor(SensorEventBroker eventBroker, int sensorId, SensorEventType sensorType)
    {
        _eventBroker = eventBroker;
        _sensorId = sensorId;
        _sensorType = sensorType;
        
        _workerThread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = $"Sensor-{sensorId}-{sensorType}"
        };
    }
    
    public void Start()
    {
        _isRunning = true;
        _workerThread.Start();
    }
    
    public void Stop()
    {
        _isRunning = false;
        _workerThread.Join(1000);
    }
    
    private void WorkerLoop()
    {
        while (_isRunning)
        {
            // Simulate reading sensor value
            double value = _random.NextDouble() * 100;
            
            // Publish the event
            _eventBroker.Publish(new SensorEvent(_sensorType, _sensorId, value));
            
            // Wait before next reading (vary by sensor type to make it more interesting)
            int delay = _sensorType == SensorEventType.Temperature ? 2000 :
                         _sensorType == SensorEventType.Proximity ? 500 :
                         _sensorType == SensorEventType.Motion ? 1000 :
                         _sensorType == SensorEventType.Position ? 100 :
                         3000; // Battery
            
            Thread.Sleep(delay);
        }
    }
}

// Example usage
public class SensorSystemDemo
{
    public static void Run()
    {
        var broker = new SensorEventBroker();
        var sensors = new List<Sensor>();
        
        // Create different types of sensors
        sensors.Add(new Sensor(broker, 1, SensorEventType.Temperature));
        sensors.Add(new Sensor(broker, 2, SensorEventType.Proximity));
        sensors.Add(new Sensor(broker, 3, SensorEventType.Motion));
        sensors.Add(new Sensor(broker, 4, SensorEventType.Position));
        sensors.Add(new Sensor(broker, 5, SensorEventType.Battery));
        
        // Subscribe to events
        broker.Subscribe(SensorEventType.Temperature, OnTemperatureEvent);
        broker.Subscribe(SensorEventType.Proximity, OnProximityEvent);
        broker.Subscribe(SensorEventType.Motion, OnAnyEvent);
        broker.Subscribe(SensorEventType.Position, OnAnyEvent);
        broker.Subscribe(SensorEventType.Battery, OnAnyEvent);
        
        // Critical alerts for all events with values > 80
        broker.Subscribe(SensorEventType.Temperature, OnCriticalAlert);
        broker.Subscribe(SensorEventType.Proximity, OnCriticalAlert);
        broker.Subscribe(SensorEventType.Motion, OnCriticalAlert);
        broker.Subscribe(SensorEventType.Position, OnCriticalAlert);
        broker.Subscribe(SensorEventType.Battery, OnCriticalAlert);
        
        // Start all sensors
        foreach (var sensor in sensors)
        {
            sensor.Start();
        }
        
        Console.WriteLine("Sensor system running. Press Enter to stop...");
        Console.ReadLine();
        
        // Stop all sensors
        foreach (var sensor in sensors)
        {
            sensor.Stop();
        }
    }
    
    private static void OnTemperatureEvent(SensorEvent e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"TEMPERATURE: {e}");
        Console.ResetColor();
    }
    
    private static void OnProximityEvent(SensorEvent e)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"PROXIMITY: {e}");
        Console.ResetColor();
    }
    
    private static void OnAnyEvent(SensorEvent e)
    {
        Console.WriteLine($"Event received: {e}");
    }
    
    private static void OnCriticalAlert(SensorEvent e)
    {
        if (e.Value > 80)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine($"CRITICAL ALERT: {e}");
            Console.ResetColor();
        }
    }
}

// Program entry point
class Program
{
    static void Main()
    {
        SensorSystemDemo.Run();
    }
}
```

### Exercise 5: Implement a simple channel with a timeout mechanism

Create a simple channel that allows producers to add items and consumers to take items with a timeout.

**Solution:**

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

public class TimeoutChannel<T>
{
    private readonly Queue<ItemWithTimestamp<T>> _queue = new Queue<ItemWithTimestamp<T>>();
    private readonly int _capacity;
    private readonly TimeSpan _itemTimeout;
    private readonly object _syncLock = new object();
    private readonly Timer _cleanupTimer;
    private bool _isCompleted;
    
    public TimeoutChannel(int capacity, TimeSpan itemTimeout)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive");
        
        _capacity = capacity;
        _itemTimeout = itemTimeout;
        
        // Create a timer to periodically clean up expired items
        _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }
    
    private class ItemWithTimestamp<TItem>
    {
        public TItem Item { get; }
        public DateTime Timestamp { get; }
        
        public ItemWithTimestamp(TItem item)
        {
            Item = item;
            Timestamp = DateTime.UtcNow;
        }
        
        public bool IsExpired(TimeSpan timeout)
        {
            return (DateTime.UtcNow - Timestamp) > timeout;
        }
    }
    
    private void CleanupExpiredItems(object state)
    {
        lock (_syncLock)
        {
            bool changed = false;
            
            // Remove expired items from the front of the queue
            while (_queue.Count > 0 && _queue.Peek().IsExpired(_itemTimeout))
            {
                _queue.Dequeue();
                changed = true;
            }
            
            // Signal waiters if we removed items and made space
            if (changed)
            {
                Monitor.PulseAll(_syncLock);
            }
        }
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
            
            _queue.Enqueue(new ItemWithTimestamp<T>(item));
            
            // Signal that data is available
            Monitor.Pulse(_syncLock);
            return true;
        }
    }
    
    public bool TryRead(out T item, int timeoutMs = Timeout.Infinite)
    {
        lock (_syncLock)
        {
            // Wait until there's non-expired data or timeout
            var startTime = Environment.TickCount;
            while (_queue.Count == 0 || _queue.Peek().IsExpired(_itemTimeout))
            {
                // Remove expired items
                while (_queue.Count > 0 && _queue.Peek().IsExpired(_itemTimeout))
                {
                    _queue.Dequeue();
                }
                
                if (_queue.Count > 0)
                    break;
                
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
            
            var itemWithTimestamp = _queue.Dequeue();
            item = itemWithTimestamp.Item;
            
            // Signal that space is available
            Monitor.Pulse(_syncLock);
            return true;
        }
    }
    
    public void Complete()
    {
        lock (_syncLock)
        {
            _isCompleted = true;
            Monitor.PulseAll(_syncLock);
            _cleanupTimer.Dispose();
        }
    }
    
    public int Count
    {
        get
        {
            lock (_syncLock)
            {
                // Count only non-expired items
                int validCount = 0;
                foreach (var item in _queue)
                {
                    if (!item.IsExpired(_itemTimeout))
                        validCount++;
                }
                return validCount;
            }
        }
    }
}
```