# Module 1: Thread Fundamentals Quiz

## Theory Questions

1. What namespace contains the Thread class in C#?

2. Describe the difference between foreground and background threads.

3. List and describe at least three possible thread states.

4. What is a race condition and why is it problematic?

5. What is the difference between using ThreadStart and ParameterizedThreadStart?

6. What is thread affinity and how does it impact performance?

## Code Exercises

1. Create a simple thread that prints "Hello from the worker thread!" to the console.

2. Create a thread that accepts a string parameter and prints it to the console.

3. The following code has a race condition. Fix it using proper synchronization.
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

4. Write code that creates two threads, waits for both to complete, and then prints "All threads completed" to the console.

5. Implement a simple producer-consumer pattern where one thread adds random numbers and another thread processes them. Use a lock for synchronization.