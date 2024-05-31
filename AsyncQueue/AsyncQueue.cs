namespace AsyncQueueSharp;

/// <summary>
/// A class for queueing asynchronous tasks.
/// </summary>
public class AsyncQueue
{
    #region Fields
    // Readonly
    private readonly Queue<IQueueItem> _responseQueue = new Queue<IQueueItem>();
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly int _maxQueueItems;

    // Public
    public bool Disposed { get; private set; }
    public int ItemsLeftToProcess { get { return _responseQueue.Count; } }

    // Private
    private bool _isProcessing = false;
    #endregion

    #region Public Methods
    /// <summary>
    /// Add a function to the queue.
    /// </summary>
    /// <typeparam name="Input">The input type.</typeparam>
    /// <typeparam name="Output">The output type.</typeparam>
    /// <param name="input">The input data.</param>
    /// <param name="onProcessStart">Called when the queue item starts processing. Returns an output.</param>
    /// <param name="onProcessComplete">Called when the queue item has completed processing and has an output.</param>
    public void Enqueue<Input, Output>(Input input, Func<Input, Task<Output>> onProcessStart, Action<Output?>? onProcessComplete = null) where Input : notnull
    {
        _ = Task.Factory.StartNew(async () =>
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_maxQueueItems > 0 && ItemsLeftToProcess >= _maxQueueItems)
                    return;
                _responseQueue.Enqueue(new QueueItem<Input, Output>(input, onProcessStart, onProcessComplete));
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    _ = ProcessResponses();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        });
    }
    /// <summary>
    /// Disposes the async queue.
    /// </summary>
    public void Dispose()
    {
        _responseQueue.Clear();
        _isProcessing = false;
        _semaphore.Dispose();
        Disposed = true;
    }
    #endregion

    #region Private Methods
    private async Task ProcessResponses()
    {
        while (true)
        {
            IQueueItem responseToProcess;

            await _semaphore.WaitAsync();
            try
            {
                if (_responseQueue.Count > 0)
                {
                    responseToProcess = _responseQueue.Dequeue();
                }
                else
                {
                    _isProcessing = false;
                    return;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            await ProcessResponse(responseToProcess);
        }
    }
    private async Task ProcessResponse(IQueueItem item)
    {
        var output = await item.ProcessStart();
        item.ProcessComplete(output);
    }
    #endregion

    #region Nested Classes
    public interface IQueueItem
    {
        public void ProcessComplete(object? data);
        public Task<object?> ProcessStart();
    }
    public class QueueItem<Input, Output> : IQueueItem where Input : notnull
    {
        /// <summary>
        /// The data of the queue item.
        /// </summary>
        public Input Data;
        /// <summary>
        /// Called when processing has completed on the queue item.
        /// </summary>
        public Action<Output?>? OnProcessComplete;
        /// <summary>
        /// Called when processing has started on the queue item.
        /// </summary>
        public Func<Input, Task<Output>> OnProcessStart;

        /// <param name="data">The data of the queue item.</param>
        /// <param name="onProcessComplete">Called when processing has completed on the queue item.</param>
        /// <param name="onProcessStart">Called when processing has started on the queue item.</param>
        public QueueItem(Input data, Func<Input, Task<Output>> onProcessStart, Action<Output?>? onProcessComplete = null)
        {
            Data = data;
            OnProcessComplete = onProcessComplete;
            OnProcessStart = onProcessStart;
        }

        public void ProcessComplete(object? data) 
        {
            if (OnProcessComplete == null)
                return;
            if (data == null)
                OnProcessComplete(default);
            if (data is Output)
                OnProcessComplete((Output)data);
        }
        public async Task<object?> ProcessStart()
        {
            return await OnProcessStart(Data);
        }
    }
    #endregion

    #region Constructor/Deconstructor
    public AsyncQueue() 
    {
        _maxQueueItems = 0;
    }
    public AsyncQueue(int maxQueueItems)
    {
        _maxQueueItems = maxQueueItems;
    }
    ~AsyncQueue()
    {
        Dispose();
    }
    #endregion
}