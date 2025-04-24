# Module 4: Robotic Control Patterns

In this module, we explore advanced threading patterns specifically designed for robotic control systems, with a focus on work dispatchers and parallel data processing. These patterns form the foundation of responsive, efficient robotic applications that can handle complex real-time operations.

## Work Dispatcher Pattern

The work dispatcher pattern provides a centralized mechanism for managing and prioritizing commands or tasks in a robot system. This is particularly important for robotics where different operations (movement, sensing, gripper control) need to be coordinated and prioritized.

### Command Base Classes

```csharp
// Base class for robot commands
public abstract class RobotCommand
{
    public int Priority { get; }
    public DateTime CreationTime { get; }
    
    public RobotCommand(int priority)
    {
        Priority = priority;
        CreationTime = DateTime.Now;
    }
    
    public abstract void Execute();
}

// Example command implementations
public class MoveCommand : RobotCommand
{
    public double X { get; }
    public double Y { get; }
    
    public MoveCommand(double x, double y, int priority) : base(priority)
    {
        X = x;
        Y = y;
    }
    
    public override void Execute()
    {
        Console.WriteLine($"Moving robot to position ({X}, {Y})");
        // Simulate movement time
        Thread.Sleep(500);
    }
}

public class GripperCommand : RobotCommand
{
    public bool Open { get; }
    
    public GripperCommand(bool open, int priority) : base(priority)
    {
        Open = open;
    }
    
    public override void Execute()
    {
        Console.WriteLine($"Gripper {(Open ? "opening" : "closing")}");
        // Simulate gripper operation
        Thread.Sleep(300);
    }
}

public class EmergencyStopCommand : RobotCommand
{
    public EmergencyStopCommand() : base(100) // Highest priority
    {
    }
    
    public override void Execute()
    {
        Console.WriteLine("EMERGENCY STOP EXECUTED");
        // Simulate emergency stop
        Thread.Sleep(100);
    }
}
```

### Basic Command Dispatcher

```csharp
// Priority-based work dispatcher
public class RobotCommandDispatcher
{
    // Custom comparer sorts by priority (descending) then by creation time (ascending)
    private readonly SortedSet<RobotCommand> _commandQueue;
    private readonly Thread _dispatcherThread;
    private readonly ManualResetEvent _newCommandEvent = new ManualResetEvent(false);
    private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private readonly object _queueLock = new object();
    private bool _isRunning;
    
    public RobotCommandDispatcher()
    {
        _commandQueue = new SortedSet<RobotCommand>(Comparer<RobotCommand>.Create((a, b) => 
        {
            int priorityComparison = b.Priority.CompareTo(a.Priority); // Higher priority first
            if (priorityComparison != 0)
                return priorityComparison;
                
            // If same priority, sort by timestamp (oldest first)
            return a.CreationTime.CompareTo(b.CreationTime);
        }));
        
        _dispatcherThread = new Thread(DispatcherLoop)
        {
            Name = "RobotCommandDispatcher",
            IsBackground = true
        };
    }
    
    public void Start()
    {
        _isRunning = true;
        _dispatcherThread.Start();
        Console.WriteLine("Robot command dispatcher started");
    }
    
    public void Stop()
    {
        _isRunning = false;
        _shutdownEvent.Set();
        _dispatcherThread.Join(1000);
        Console.WriteLine("Robot command dispatcher stopped");
    }
    
    public void EnqueueCommand(RobotCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));
            
        lock (_queueLock)
        {
            _commandQueue.Add(command);
            
            // Log high-priority commands
            if (command.Priority > 50)
            {
                Console.WriteLine($"High priority command enqueued: {command.GetType().Name}, Priority: {command.Priority}");
            }
        }
        
        // Signal that a new command is available
        _newCommandEvent.Set();
    }
    
    private void DispatcherLoop()
    {
        WaitHandle[] waitHandles = new WaitHandle[] { _newCommandEvent, _shutdownEvent };
        
        while (_isRunning)
        {
            // Wait for a new command or shutdown
            int index = WaitHandle.WaitAny(waitHandles, 1000);
            
            if (index == 1) // Shutdown event
                break;
                
            // Process commands if there are any
            ProcessCommands();
            
            // Reset the event after processing
            _newCommandEvent.Reset();
        }
    }
    
    private void ProcessCommands()
    {
        while (true)
        {
            RobotCommand nextCommand = null;
            
            lock (_queueLock)
            {
                if (_commandQueue.Count == 0)
                    break;
                
                // Get the highest priority command
                nextCommand = _commandQueue.Min; // Due to our comparer, this is actually the highest priority
                _commandQueue.Remove(nextCommand);
            }
            
            if (nextCommand != null)
            {
                // Execute the command
                Console.WriteLine($"Executing command: {nextCommand.GetType().Name}, Priority: {nextCommand.Priority}");
                try
                {
                    nextCommand.Execute();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing command: {ex.Message}");
                }
            }
        }
    }
    
    public int PendingCommandCount
    {
        get
        {
            lock (_queueLock)
            {
                return _commandQueue.Count;
            }
        }
    }
}
```

### Advanced Command Dispatcher with Timeouts and Cancellation

For more complex robotics applications, we need to handle timeouts and cancellation:

```csharp
// Command base class with timeout and cancellation support
public abstract class RobotCommand
{
    public int Priority { get; }
    public DateTime CreationTime { get; }
    public TimeSpan Timeout { get; }
    public CancellationToken CancellationToken { get; }
    
    public RobotCommand(int priority, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        Priority = priority;
        CreationTime = DateTime.Now;
        Timeout = timeout;
        CancellationToken = cancellationToken;
    }
    
    public abstract bool Execute();
    
    public bool IsExpired => DateTime.Now - CreationTime > Timeout;
}

// Example command implementation with cancellation support
public class MoveCommand : RobotCommand
{
    public double X { get; }
    public double Y { get; }
    
    public MoveCommand(double x, double y, int priority, TimeSpan timeout, 
                       CancellationToken cancellationToken = default) 
        : base(priority, timeout, cancellationToken)
    {
        X = x;
        Y = y;
    }
    
    public override bool Execute()
    {
        Console.WriteLine($"Moving robot to position ({X}, {Y})");
        
        // Simulate movement that takes time and checks for cancellation
        for (int i = 0; i < 10; i++)
        {
            // Check for cancellation
            if (CancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Move command was cancelled");
                return false;
            }
            
            // Simulate a step of the movement
            Console.WriteLine($"  Movement progress: {(i + 1) * 10}%");
            Thread.Sleep(100);
        }
        
        Console.WriteLine("Move command completed successfully");
        return true;
    }
}
```

## Parallel Data Processing Pipeline

Robotic systems often need to process sensor data through multiple stages. A pipeline architecture allows for concurrent processing across stages:

### Pipeline Stage Base Class

```csharp
// Base class for pipeline stages
public abstract class PipelineStage<TInput, TOutput>
{
    protected readonly BlockingCollection<TInput> _inputQueue = new BlockingCollection<TInput>();
    protected readonly BlockingCollection<TOutput> _outputQueue;
    protected readonly CancellationToken _cancellationToken;
    protected readonly Thread[] _workerThreads;
    protected readonly string _stageName;
    
    protected PipelineStage(BlockingCollection<TOutput> outputQueue, string stageName, 
                            int numThreads, CancellationToken cancellationToken)
    {
        _outputQueue = outputQueue;
        _stageName = stageName;
        _cancellationToken = cancellationToken;
        
        _workerThreads = new Thread[numThreads];
        for (int i = 0; i < numThreads; i++)
        {
            int threadNum = i;
            _workerThreads[i] = new Thread(() => WorkerLoop(threadNum))
            {
                Name = $"{_stageName}-Worker-{i}",
                IsBackground = true
            };
        }
    }
    
    public void Start()
    {
        Console.WriteLine($"Starting pipeline stage: {_stageName}");
        foreach (var thread in _workerThreads)
        {
            thread.Start();
        }
    }
    
    public void Stop()
    {
        _inputQueue.CompleteAdding();
        foreach (var thread in _workerThreads)
        {
            thread.Join(1000);
        }
    }
    
    public void ProcessItem(TInput item)
    {
        if (!_inputQueue.IsAddingCompleted)
        {
            _inputQueue.Add(item);
        }
    }
    
    protected abstract TOutput Process(TInput input);
    
    private void WorkerLoop(int threadNum)
    {
        try
        {
            foreach (var item in _inputQueue.GetConsumingEnumerable(_cancellationToken))
            {
                try
                {
                    var result = Process(item);
                    if (result != null && !_outputQueue.IsAddingCompleted)
                    {
                        _outputQueue.Add(result, _cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"{_stageName} processing cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in {_stageName}: {ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"{_stageName} worker cancelled");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in {_stageName} worker loop: {ex.Message}");
        }
        
        Console.WriteLine($"{_stageName} worker {threadNum} finished");
    }
}
```

### Sensor Data and Processing Stages

```csharp
// Sensor data class
public class SensorData
{
    public int SensorId { get; set; }
    public DateTime Timestamp { get; set; }
    public double[] RawValues { get; set; }
    public double[] FilteredValues { get; set; }
    public double[] NormalizedValues { get; set; }
    public Dictionary<string, object> Features { get; set; }
    public string Classification { get; set; }
    
    public SensorData(int sensorId, double[] rawValues)
    {
        SensorId = sensorId;
        Timestamp = DateTime.Now;
        RawValues = rawValues;
        Features = new Dictionary<string, object>();
    }
    
    public override string ToString()
    {
        return $"Sensor {SensorId} [{Timestamp:HH:mm:ss.fff}] - Classification: {Classification ?? "Unknown"}";
    }
}

// Example processing stages
public class FilterStage : PipelineStage<SensorData, SensorData>
{
    public FilterStage(BlockingCollection<SensorData> outputQueue, CancellationToken cancellationToken)
        : base(outputQueue, "Filter", 2, cancellationToken)
    {
    }
    
    protected override SensorData Process(SensorData input)
    {
        Console.WriteLine($"Filtering data from Sensor {input.SensorId}");
        
        // Simulate filtering
        input.FilteredValues = new double[input.RawValues.Length];
        for (int i = 0; i < input.RawValues.Length; i++)
        {
            // Simple filter: remove noise by capping extreme values
            double value = input.RawValues[i];
            if (value > 100) value = 100;
            if (value < 0) value = 0;
            input.FilteredValues[i] = value;
        }
        
        // Simulate processing time
        Thread.Sleep(100);
        
        return input;
    }
}

public class NormalizationStage : PipelineStage<SensorData, SensorData>
{
    public NormalizationStage(BlockingCollection<SensorData> outputQueue, CancellationToken cancellationToken)
        : base(outputQueue, "Normalization", 2, cancellationToken)
    {
    }
    
    protected override SensorData Process(SensorData input)
    {
        Console.WriteLine($"Normalizing data from Sensor {input.SensorId}");
        
        if (input.FilteredValues == null)
        {
            throw new InvalidOperationException("Cannot normalize unfiltered data");
        }
        
        // Find min and max values
        double min = input.FilteredValues.Min();
        double max = input.FilteredValues.Max();
        double range = max - min;
        
        // Normalize to range [0,1]
        input.NormalizedValues = new double[input.FilteredValues.Length];
        for (int i = 0; i < input.FilteredValues.Length; i++)
        {
            input.NormalizedValues[i] = range == 0 ? 0 : (input.FilteredValues[i] - min) / range;
        }
        
        Thread.Sleep(150);
        
        return input;
    }
}
```

## Real-Time Data Transformation Chain

For applications where minimal latency is critical, we can implement a dedicated real-time transformation chain:

```csharp
// Define a delegate for transformation functions
public delegate double[] TransformFunction(double[] input);

// A class representing a single transformation in the chain
public class DataTransformer
{
    private readonly string _name;
    private readonly TransformFunction _transform;
    
    public DataTransformer(string name, TransformFunction transform)
    {
        _name = name;
        _transform = transform;
    }
    
    public double[] Transform(double[] input)
    {
        // Apply the transformation
        double[] result = _transform(input);
        
        // For debugging/monitoring
        Console.WriteLine($"Transformation '{_name}' applied");
        
        return result;
    }
    
    public string Name => _name;
}

// The transformation chain that processes data in real-time
public class RealTimeTransformationChain
{
    private readonly List<DataTransformer> _transformers = new List<DataTransformer>();
    private readonly Thread _processingThread;
    private readonly ManualResetEvent _dataAvailableEvent = new ManualResetEvent(false);
    private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private readonly object _dataLock = new object();
    
    private double[] _currentInput;
    private double[] _latestOutput;
    private bool _isProcessing;
    private bool _isRunning;
    
    // Performance metrics
    private DateTime _lastProcessingTime;
    private long _processingCount;
    private TimeSpan _totalProcessingTime;
    private TimeSpan _maxProcessingTime;
    
    public RealTimeTransformationChain()
    {
        _processingThread = new Thread(ProcessingLoop)
        {
            Name = "TransformationChain",
            IsBackground = true
        };
    }
    
    public void AddTransformer(DataTransformer transformer)
    {
        _transformers.Add(transformer);
        Console.WriteLine($"Added transformer: {transformer.Name}");
    }
    
    public void Start()
    {
        if (_transformers.Count == 0)
        {
            throw new InvalidOperationException("Cannot start a transformation chain with no transformers");
        }
        
        _isRunning = true;
        _processingThread.Start();
        Console.WriteLine("Real-time transformation chain started");
    }
    
    public void Stop()
    {
        _isRunning = false;
        _shutdownEvent.Set();
        _processingThread.Join(1000);
        Console.WriteLine("Real-time transformation chain stopped");
        
        // Print performance metrics
        if (_processingCount > 0)
        {
            Console.WriteLine($"Processing statistics:");
            Console.WriteLine($"  - Total processed: {_processingCount}");
            Console.WriteLine($"  - Avg processing time: {_totalProcessingTime.TotalMilliseconds / _processingCount:F2} ms");
            Console.WriteLine($"  - Max processing time: {_maxProcessingTime.TotalMilliseconds:F2} ms");
        }
    }
    
    public void ProcessData(double[] input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));
            
        lock (_dataLock)
        {
            _currentInput = input;
            _isProcessing = true;
        }
        
        _dataAvailableEvent.Set();
    }
    
    public double[] GetLatestOutput()
    {
        lock (_dataLock)
        {
            return _latestOutput?.Clone() as double[];
        }
    }
    
    private void ProcessingLoop()
    {
        WaitHandle[] waitHandles = new WaitHandle[] { _dataAvailableEvent, _shutdownEvent };
        
        while (_isRunning)
        {
            int index = WaitHandle.WaitAny(waitHandles);
            
            if (index == 1) // Shutdown event
                break;
                
            double[] inputData;
            lock (_dataLock)
            {
                if (!_isProcessing)
                {
                    _dataAvailableEvent.Reset();
                    continue;
                }
                
                inputData = _currentInput.Clone() as double[];
                _isProcessing = false;
                _dataAvailableEvent.Reset();
            }
            
            // Process the data through the transformation chain
            var startTime = DateTime.Now;
            double[] result = ProcessThroughChain(inputData);
            var processingTime = DateTime.Now - startTime;
            
            // Update performance metrics
            _processingCount++;
            _totalProcessingTime += processingTime;
            if (processingTime > _maxProcessingTime)
                _maxProcessingTime = processingTime;
                
            // Store the result
            lock (_dataLock)
            {
                _latestOutput = result;
                _lastProcessingTime = DateTime.Now;
            }
        }
    }
    
    private double[] ProcessThroughChain(double[] input)
    {
        double[] current = input;
        
        foreach (var transformer in _transformers)
        {
            current = transformer.Transform(current);
        }
        
        return current;
    }
    
    public bool IsOutputCurrent
    {
        get
        {
            lock (_dataLock)
            {
                if (_latestOutput == null || _lastProcessingTime == default)
                    return false;
                    
                // Consider output current if processed within last 100ms
                return (DateTime.Now - _lastProcessingTime).TotalMilliseconds < 100;
            }
        }
    }
}
```

## Result Aggregation from Multiple Sources

In advanced robotic systems, we often need to combine results from multiple sensors or processing units:

```csharp
// Represents a sensor reading
public class SensorReading
{
    public int SensorId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; }
    
    public override string ToString()
    {
        return $"Sensor {SensorId}: {Value} {Unit} at {Timestamp:HH:mm:ss.fff}";
    }
}

// Sensor data aggregator for combining multiple sensors
public class SensorDataAggregator
{
    private readonly ConcurrentDictionary<int, SensorReading> _latestReadings = new ConcurrentDictionary<int, SensorReading>();
    private readonly Thread _aggregationThread;
    private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    private readonly TimeSpan _aggregationInterval;
    private readonly List<ISensorDataConsumer> _consumers = new List<ISensorDataConsumer>();
    private readonly object _consumersLock = new object();
    private bool _isRunning;
    
    // Interface for consumers of aggregated data
    public interface ISensorDataConsumer
    {
        void ConsumeAggregatedData(IReadOnlyDictionary<int, SensorReading> readings, DateTime aggregationTime);
    }
    
    public SensorDataAggregator(TimeSpan aggregationInterval)
    {
        _aggregationInterval = aggregationInterval;
        _aggregationThread = new Thread(AggregationLoop)
        {
            Name = "SensorAggregator",
            IsBackground = true
        };
    }
    
    public void Start()
    {
        _isRunning = true;
        _aggregationThread.Start();
        Console.WriteLine("Sensor data aggregator started");
    }
    
    public void Stop()
    {
        _isRunning = false;
        _shutdownEvent.Set();
        _aggregationThread.Join(1000);
        Console.WriteLine("Sensor data aggregator stopped");
    }
    
    public void RegisterConsumer(ISensorDataConsumer consumer)
    {
        lock (_consumersLock)
        {
            _consumers.Add(consumer);
        }
    }
    
    public void UnregisterConsumer(ISensorDataConsumer consumer)
    {
        lock (_consumersLock)
        {
            _consumers.Remove(consumer);
        }
    }
    
    // Process a new sensor reading
    public void ProcessReading(SensorReading reading)
    {
        // Update the latest reading for this sensor
        _latestReadings[reading.SensorId] = reading;
    }
    
    private void AggregationLoop()
    {
        while (_isRunning)
        {
            // Wait for shutdown or interval
            if (_shutdownEvent.WaitOne(_aggregationInterval))
                break;
            
            // Create a snapshot of the current readings
            Dictionary<int, SensorReading> snapshot = new Dictionary<int, SensorReading>();
            foreach (var pair in _latestReadings)
            {
                snapshot[pair.Key] = pair.Value;
            }
            
            // Get current timestamp
            var aggregationTime = DateTime.Now;
            
            // Send snapshot to all consumers
            List<ISensorDataConsumer> consumersCopy;
            lock (_consumersLock)
            {
                consumersCopy = new List<ISensorDataConsumer>(_consumers);
            }
            
            foreach (var consumer in consumersCopy)
            {
                try
                {
                    consumer.ConsumeAggregatedData(snapshot, aggregationTime);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in consumer: {ex.Message}");
                }
            }
        }
    }
    
    public IReadOnlyDictionary<int, SensorReading> GetCurrentReadings()
    {
        return new Dictionary<int, SensorReading>(_latestReadings);
    }
}
```

## Summary

In this module, we've explored advanced threading patterns for robotic control systems:

1. **Work Dispatcher Pattern**: Provides centralized command management with priority-based execution, timeout handling, and cancellation support.

2. **Parallel Data Processing Pipeline**: Enables efficient multi-stage processing of sensor data with independent stages running concurrently.

3. **Real-Time Data Transformation Chain**: Optimized for minimal latency in time-critical applications.

4. **Result Aggregation**: Combines data from multiple sources to make higher-level decisions.

These patterns can be mixed and matched to build complex robotic systems that respond rapidly to inputs while maintaining stability and performance. When implementing these patterns, remember to:

- Balance thread usage with your hardware capabilities
- Always handle thread safety appropriately
- Consider timeouts and cancellation for all operations
- Monitor and measure performance to identify bottlenecks