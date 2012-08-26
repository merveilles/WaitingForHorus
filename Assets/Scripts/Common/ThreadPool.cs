using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

public class ThreadPool : MonoBehaviour, IThreadPool
{
    readonly Stack<PersistentThread> stack = new Stack<PersistentThread>();
    bool disposed;

    static IThreadPool instance;
    public static IThreadPool Instance
    {
        get { return instance; }
    }
    void Awake()
    {
        if (instance == null) instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void OnApplicationQuit()
    {
        instance = null;
        while (stack.Count > 0) stack.Pop().Dispose();
        disposed = true;
    }

    static PersistentThread CreateThread()
    {
        return new PersistentThread();
    }

    public void Fire(Action task) { Fire(task, ThreadPriority.Normal); }
    public void Fire(Action task, ThreadPriority priority)
    {
        ActiveThreads++;
        var thread = stack.Count > 0 ? stack.Pop() : CreateThread();
        //Debug.Log("thread is started? " + thread.Started);
        //Debug.Log("thread has worker?" + (thread.CurrentWorker != null));
        new Worker<bool>(this, thread, dummy => task()) { Priority = priority }.Start(false);
    }

    public void Fire<TContext>(Action<TContext> task, TContext context) { Fire(task, context, ThreadPriority.Normal); }
    public void Fire<TContext>(Action<TContext> task, TContext context, ThreadPriority priority)
    {
        ActiveThreads++;
        var thread = stack.Count > 0 ? stack.Pop() : CreateThread();
        new Worker<TContext>(this, thread, task) { Priority = priority }.Start(context);
    }

    public IFuture<TResult> Evaluate<TResult>(Func<TResult> task) { return Evaluate(task, ThreadPriority.Normal); }
    public IFuture<TResult> Evaluate<TResult>(Func<TResult> task, ThreadPriority priority)
    {
        ActiveThreads++;
        var thread = stack.Count > 0 ? stack.Pop() : CreateThread();
        return new Worker<bool, TResult>(this, thread, dummy => task()) { Priority = priority }.Start(false);
    }

    public IFuture<TResult> Evaluate<TContext, TResult>(Func<TContext, TResult> task, TContext context) { return Evaluate(task, context, ThreadPriority.Normal); }
    public IFuture<TResult> Evaluate<TContext, TResult>(Func<TContext, TResult> task, TContext context, ThreadPriority priority)
    {
        ActiveThreads++;
        var thread = stack.Count > 0 ? stack.Pop() : CreateThread();
        return new Worker<TContext, TResult>(this, thread, task) { Priority = priority }.Start(context);
    }

    internal void Return(WorkerBase worker)
    {
        if (disposed)   worker.UnderlyingThread.Dispose();
        else            stack.Push(worker.UnderlyingThread);
        ActiveThreads--;
    }

    public int ActiveThreads { get; private set; }
}

public interface IThreadPool
{
    void Fire(Action task);
    void Fire(Action task, ThreadPriority priority);

    void Fire<TContext>(Action<TContext> task, TContext context);
    void Fire<TContext>(Action<TContext> task, TContext context, ThreadPriority priority);

    IFuture<TResult> Evaluate<TResult>(Func<TResult> task);
    IFuture<TResult> Evaluate<TResult>(Func<TResult> task, ThreadPriority priority);

    IFuture<TResult> Evaluate<TContext, TResult>(Func<TContext, TResult> task, TContext context);
    IFuture<TResult> Evaluate<TContext, TResult>(Func<TContext, TResult> task, TContext context, ThreadPriority priority);

    int ActiveThreads { get; }
}

class PersistentThread : IDisposable
{
    readonly Thread thread;
    readonly ManualResetEvent startEvent, joinEvent;

    public bool Started { get; private set; }
    public bool Disposed { get; private set; }

    public IWorker CurrentWorker { internal get; set; }

    public PersistentThread()
    {
        startEvent = new ManualResetEvent(false);
        joinEvent = new ManualResetEvent(false);

        thread = new Thread(DoWork);
        thread.Start();
    }

    public void Start()
    {
        Started = true;

        startEvent.Set();
    }

    public void Join()
    {
        joinEvent.WaitOne();
        joinEvent.Reset();
    }

    void DoWork()
    {
        startEvent.WaitOne();
        startEvent.Reset();

        while (!Disposed)
        {
            CurrentWorker.Act();

            joinEvent.Set();
            Started = false;
            startEvent.WaitOne();
            startEvent.Reset();
        }
    }

    public ThreadPriority Priority
    {
        set { thread.Priority = value; }
    }

    public void Dispose()
    {
        if (!Disposed)
            GC.SuppressFinalize(this);

        DisposeInternal();
    }

    void DisposeInternal()
    {
        if (!Disposed)
        {
            Disposed = true;
            startEvent.Set();
        }
    }

    ~PersistentThread()
    {
        DisposeInternal();
    }
}

interface IWorker
{
    void Act();
}

public interface IFuture<T>
{
    bool HasValue { get; }
    bool InError { get; }
    Exception Exception { get; }
    T Value { get; }
}

public class Worker<TContext> : WorkerBase
{
    readonly Action<TContext> task;

    TContext cachedContext;

    internal Worker(ThreadPool pool, PersistentThread thread, Action<TContext> task) : base(pool, thread)
    {
        this.task = task;
    }

    public override void Act()
    {
        try
        {
            task(cachedContext);
        }
        catch (Exception ex)
        {
            Exception = ex;
            InError = true;
        }
        End();
    }

    public void Start(TContext context)
    {
        if (thread.Started)     throw new InvalidOperationException("Thread is already started");
        if (thread.Disposed)    throw new ObjectDisposedException("PersistentThread");

        cachedContext = context;
        thread.CurrentWorker = this;

        thread.Start();
    }
}

public class Worker<TContext, TResult> : WorkerBase, IFuture<TResult>
{
    readonly Func<TContext, TResult> task;

    TContext cachedContext;
    TResult result;

    public bool HasValue { get; private set; }

    internal Worker(ThreadPool pool, PersistentThread thread, Func<TContext, TResult> task) : base(pool, thread)
    {
        this.task = task;
    }

    public override void Act()
    {
        try
        {
            result = task(cachedContext);
            HasValue = true;
        }
        catch (Exception ex) 
        {
            Exception = ex;
            InError = true;
        }
        End();
    }

    public IFuture<TResult> Start(TContext context)
    {
        if (thread.Started)     throw new InvalidOperationException("Thread is already started");
        if (thread.Disposed)    throw new ObjectDisposedException("PersistentThread");

        cachedContext = context;
        thread.CurrentWorker = this;

        thread.Start();

        return this;
    }

    public TResult Value
    {
        get
        {
            if (!HasValue) Join();
            return result;
        }
    }
}

public abstract class WorkerBase : IWorker
{
    readonly internal PersistentThread thread;
    readonly ThreadPool pool;

    public bool InError { get; protected set; }
    public Exception Exception { get; protected set; }

    internal WorkerBase(ThreadPool pool, PersistentThread thread)
    {
        this.thread = thread;
        this.pool = pool;
    }

    public ThreadPriority Priority
    {
        set { thread.Priority = value; }
    }

    internal PersistentThread UnderlyingThread
    {
        get { return thread; }
    }

    public abstract void Act();

    protected void End()
    {
        thread.Priority = ThreadPriority.Normal;
        pool.Return(this);
    }

    public void Join()
    {
        if (!thread.Started)
            // Idle thread, no join needed
            return;

        if (thread.Disposed)
            throw new ObjectDisposedException("PersistentThread");

        thread.Join();
    }
}
