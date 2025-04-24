# Module 2: Synchronization Mechanisms Quiz

## Theory Questions

1. What is the difference between the lock statement and directly using the Monitor class?

2. Explain the difference between ManualResetEvent and AutoResetEvent.

3. When would you use Interlocked operations instead of a lock statement?

4. What is a deadlock and how can it be prevented?

5. What is the purpose of ReaderWriterLockSlim and when would you use it?

6. Explain the purpose of the Barrier class and provide an example scenario where it would be useful.

## Code Exercises

1. Fix a potential deadlock in the provided code.
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

2. Write code that creates three worker threads that each perform a task, and then uses a ManualResetEvent to signal all threads to start simultaneously.

3. Create a thread-safe singleton class using the double-check locking pattern.

4. Implement a bounded buffer using Monitor's Wait() and Pulse() methods for synchronization.

5. Implement a thread-safe cache that uses ReaderWriterLockSlim to allow concurrent reads but exclusive writes.