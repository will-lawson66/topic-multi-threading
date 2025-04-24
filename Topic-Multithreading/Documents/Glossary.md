# Glossary of Multi-threading Terms

## Core Concepts
- **Thread**: A lightweight execution path within a process, with its own stack and processor state
- **Main Thread**: The primary execution thread created when a C# application starts
- **Thread Pool**: A collection of worker threads managed by the CLR to optimize thread usage
- **Context Switch**: When the CPU switches from executing one thread to another
- **Thread Affinity**: The association between a thread and a specific CPU core
- **CPU-bound**: Operations that primarily use processor resources
- **I/O-bound**: Operations that primarily wait for external resources (disk, network, etc.)

## Thread States
- **Unstarted**: Thread is created but not yet started
- **Running**: Thread is executing
- **WaitSleepJoin**: Thread is blocked (waiting, sleeping, or joined to another thread)
- **Suspended**: Thread execution is temporarily halted
- **Aborted**: Thread is in the process of terminating
- **Stopped**: Thread has completed execution

## Synchronization Mechanisms
- **lock statement**: Simplest synchronization mechanism (uses Monitor internally)
- **Monitor**: Provides a mechanism for synchronizing access to a region of code
- **Mutex**: Synchronization primitive that can be used across processes
- **Semaphore**: Controls access to a resource or pool of resources
- **ManualResetEvent/AutoResetEvent**: Signal completion of operations between threads
- **SpinLock**: Low-level synchronization that spins while waiting for lock acquisition
- **ReaderWriterLock**: Allows concurrent read access but exclusive write access
- **Barrier**: Enables multiple threads to work on an algorithm in phases
- **Interlocked**: Provides atomic operations for variables shared by multiple threads

## Thread Issues
- **Race Condition**: When the behavior depends on the relative timing of events
- **Deadlock**: When two or more threads wait for each other, blocking progress
- **Livelock**: Threads actively try to resolve a conflict but prevent progress
- **Thread Starvation**: A thread cannot get sufficient resources to proceed
- **Priority Inversion**: When a high-priority thread waits for a low-priority thread

## Thread-Safe Collections
- **ConcurrentDictionary**: Thread-safe implementation of a dictionary
- **ConcurrentQueue**: Thread-safe implementation of a FIFO collection
- **ConcurrentStack**: Thread-safe implementation of a LIFO collection
- **ConcurrentBag**: Thread-safe unordered collection
- **BlockingCollection**: Provides blocking and bounding capabilities for collections