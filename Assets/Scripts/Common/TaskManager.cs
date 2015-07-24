using UnityEngine;
using System;
using System.Collections.Generic;

// delegate used by task queue/manager system
public delegate bool UntilTaskPredicate(float elapsedTime);

public class TaskQueue
{
    // manage each step of the task queue
    private enum StepType { Action, TimeWait, ConditionWait }
    private class Step
    {
        // type of step and timer for tracking how long we've been running the step
        public StepType Type;
        public float Timer;

        // one of these three will matter depending on the step type
        public Action Action;
        public float Length;
        public UntilTaskPredicate Condition;
    }

    // cache of steps we can reuse to avoid garbage
    private static readonly Stack<Step> stepCache = new Stack<Step>();

    // steps for this queue
    private readonly Queue<Step> steps = new Queue<Step>();

    // whether or not the queue is complete
    public bool IsComplete { get { return steps.Count == 0; } }

    // updates the task queue
    public void Update()
    {
        // if we're out of steps, we can't update anything
        if (steps.Count == 0)
            return;

        // see the next step and update its timer
        Step s = steps.Peek();
        s.Timer += Time.deltaTime;

        // handle the step
        switch (s.Type)
        {
            // actions just get invoked
            case StepType.Action:
                s.Action();
                s.Action = null;
                stepCache.Push(steps.Dequeue());
                break;

            // timers check for a specific time before moving on
            case StepType.TimeWait:
                if (s.Timer >= s.Length)
                    stepCache.Push(steps.Dequeue());
                break;

            // conditions require a delegate to return true to move on
            case StepType.ConditionWait:
                if (s.Condition(s.Timer))
                {
                    s.Condition = null;
                    stepCache.Push(steps.Dequeue());
                }
                break;

            default:
                break;
        }
    }

    // adds an action to the queue
    public TaskQueue Then(Action action)
    {
        Step s = NewStep();
        s.Type = StepType.Action;
        s.Action = action;
        steps.Enqueue(s);
        return this;
    }

    // adds a timer to the queue
    public TaskQueue ThenWaitFor(float seconds)
    {
        Step s = NewStep();
        s.Type = StepType.TimeWait;
        s.Length = seconds;
        steps.Enqueue(s);
        return this;
    }

    // adds a conditional action to the queue
    public TaskQueue ThenWaitUntil(UntilTaskPredicate condition)
    {
        Step s = NewStep();
        s.Type = StepType.ConditionWait;
        s.Condition = condition;
        steps.Enqueue(s);
        return this;
    }

    // stops a task queue
    public void Stop()
    {
        while (steps.Count > 0)
        {
            Step s = steps.Dequeue();
            s.Action = null;
            s.Condition = null;
            stepCache.Push(s);
        }
    }

    // helper for getting a new step
    private static Step NewStep()
    {
        Step step = stepCache.Count > 0 ? stepCache.Pop() : new Step();
        step.Timer = 0f;
        return step;
    }
}

public class TaskManager : MonoBehaviour, ITaskManager
{
    // the first TaskManager to Awake() is the main one
    static ITaskManager instance;
    public static ITaskManager Instance
    {
        get { return instance; }
    }

    // cache of queues we can reuse to avoid garbage
    readonly Stack<TaskQueue> queueCache = new Stack<TaskQueue>();

    // active queues
    readonly List<TaskQueue> queues = new List<TaskQueue>();

    void Awake()
    {
        // if we're first, we're main
        if (instance == null) instance = this;
        DontDestroyOnLoad(gameObject);
    }
    void OnApplicationQuit()
    {
        instance = null;
    }

    void Update()
    {
        // update all queues (backwards since we will be modifying the list)
        for (int i = queues.Count - 1; i >= 0; i--)
        {
            queues[i].Update();

            // if the queue is complete put it in the cache and remove it from the list
            if (queues[i].IsComplete)
            {
                queueCache.Push(queues[i]);
                queues.RemoveAt(i);
            }
        }
    }

    public void StopAllTaskQueues()
    {
        // stop each queue and put it into the cache
        foreach (var q in queues)
        {
            q.Stop();
            queueCache.Push(q);
        }

        // clear our active list
        queues.Clear();
    }

    public TaskQueue WaitFor(float seconds)
    {
        // find a cached queue or make a new one
        TaskQueue queue = queueCache.Count > 0 ? queueCache.Pop() : new TaskQueue();

        // store the queue in our active list
        queues.Add(queue);

        // queue up a wait action and hand it back to the caller
        return queue.ThenWaitFor(seconds);
    }

    public TaskQueue WaitUntil(UntilTaskPredicate condition)
    {
        // find a cached queue or make a new one
        TaskQueue queue = queueCache.Count > 0 ? queueCache.Pop() : new TaskQueue();

        // store the queue in our active list
        queues.Add(queue);

        // queue up a conditional action and hand it back to the caller
        return queue.ThenWaitUntil(condition);
    }
}
public interface ITaskManager
{
    void StopAllTaskQueues();
    TaskQueue WaitFor(float seconds);
    TaskQueue WaitUntil(UntilTaskPredicate condition);
}