# ⏳Async Queue Sharp
Async Queue Sharp is a lightweight C# library for queueing asynchronous tasks.

<a href="https://www.nuget.org/packages/AsyncQueueSharp/1.0.1">
    <img alt="NuGet Version" src="https://img.shields.io/nuget/v/AsyncQueueSharp">
</a>

## 📖 Documentation
### Full Example
```c#
using AsyncQueueSharp;

public static class Program
{
    public static AsyncQueue Queue = new AsyncQueue(); // Create async queue instance

    public static void Main(string[] args)
    {
        for (int i = 0; i < 3; i++) // Create 3 async operations
        {
            Queue.Enqueue<int>(10, OnProcess); // Enqueue the operation
        }
        Console.ReadLine();
    }

    static async Task OnProcess(int input) // Called when the enqueued operation reaches the front of the queue and start processing
    {
        Console.WriteLine("Processing input: " + input);
        await Task.Delay(1000); // Simulate 1 second operation
        Console.WriteLine("Output is: " + (input - 3));
    }
}
```
---
### Async Queue Class
This creates an async queue with no operation limit. This means you can have as many operations queued as you like.
```c#
public static AsyncQueue Queue = new AsyncQueue();
```
This creates an async queue with an operation limit of 5. This limits the amount of operations in the async queue to 5.
```c#
public static AsyncQueue Queue = new AsyncQueue(5);
```
---
### Enqueue Method
This is the method structure.
```c#
void Enqueue<Input>(Input input, Func<Input, Task> onProcess)
```
This enqueues an operation with a int as the input.
```c#
AsyncQueue.Enqueue<int>(10, OnProcess);

async Task OnProcess(int input)...
```
This enqueues an operation with a string as the input.
```c#
AsyncQueue.Enqueue<string>("Hello World", OnProcess);

async Task OnProcess(string input)...
```
