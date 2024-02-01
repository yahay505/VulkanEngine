﻿using System.Diagnostics;

namespace VulkanEngine.ECS_internals;

public class WorkerThread
{
    public Dictionary<int,int> ManagedThreadIDToUnmanagedThreadID = new();
    
    Thread SystemThread = null!;
    [ThreadStatic]
    public static int ThreadID;
    public static bool IsMainThread => ThreadID == 0;
    public WorkerWorkStack WorkStack = new();

    private WorkerThread(int threadId)
    {
        ThreadID = threadId;
    }

    public static WorkerThread Create(int id,bool SummonThread=true)
    {
        var t = new WorkerThread(id);
        
        if (SummonThread)
        {
            t.SystemThread = new Thread(t.entryPoint);
            t.SystemThread.Priority = ThreadPriority.Highest;

            // set affinity target
            t.SystemThread.Start();
            
        }
        // throw new NotImplementedException();
        return t;
    }

    internal unsafe void entryPoint()
    {
        // Thread.BeginThreadAffinity();
        
        // ManagedThreadIDToUnmanagedThreadID.Add(Thread.CurrentThread.ManagedThreadId,);
        
        SpinWait.SpinUntil(() => Volatile.Read(ref Scheduler.WorkerThreadEnable));
        // var Work = 0;
        while (true)
        {
            if (WorkStack.WorkerTryPop(out var job))
            {
                job.Function();
                // release resources
                foreach (var ecsResource in job.Writes)
                {
                    Volatile.Write(ref ecsResource.state, 0);
                }
                foreach (var ecsResource in job.Reads)
                {
                    Interlocked.Decrement(ref ecsResource.state);
                }
                Volatile.Write(ref job.IsCompleted,true);
                if (Scheduler.Stopping)
                {
                    return;
                }
            }
            else
            {
                if(TrySteal((ThreadID + 1)% Scheduler.ThreadCount)) continue;
                if(Scheduler.TrySchedule()) continue;

                for (int i = 0; i < Scheduler.threads.Length; i++)
                {
                    if (i == ThreadID)
                    {
                        continue;
                    }
                    if (TrySteal(i))
                    {
                        continue;
                    }
                }
                // if all else fails, wait for work or scheduler
                // alloc is ok as we have nothing to do
                SpinWait.SpinUntil(() => WorkStack.WorkerHasAny() || Volatile.Read(ref Scheduler.ShouldSync) || Scheduler.TrySchedule());
                if (Scheduler.ShouldSync)
                {
                    Sync();
                    if (Scheduler.Stopping)
                    {
                        return;
                    }
                }
                continue;
            }
        }
        Console.WriteLine($"thread {ThreadID} exited");
    }
    private bool TrySteal(int i)
    {
        
        
        return Scheduler.threads[i].WorkStack.Steal();
    }
    private void Sync()
    {
        Scheduler.SyncBarrier.SignalAndWait();
    }
}