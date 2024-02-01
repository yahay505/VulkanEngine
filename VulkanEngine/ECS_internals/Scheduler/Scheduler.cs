using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public static class Scheduler
{
    public static int schedulerLock = 0;
    public static WorkerThread[] threads = null!;
    public static int ThreadCount = -1;
    public static bool WorkerThreadEnable = false;
    public static bool ShouldSync = false;
    public static Barrier SyncBarrier = null!;
    public static bool Stopping = false;
    
    public static Dictionary<string,RuntimeScheduleState> RuntimeScheduleStates = null!;
    private static RuntimeScheduleState _currentRuntimeScheduleState = null!;

    public static IEnumerable<string> TargetEnumarable=null!;
    private static IEnumerator<string> currentTargetEnumerator=null!;
    private static RuntimeScheduleItem[] __runtimeScheduleItems = new RuntimeScheduleItem[20];

    static Scheduler()
    {
        // CollectViaReflection();
    }
    
    public static int CalculateThreadCount()
    {
        return 4;
    }
    public static void Stop()
    {
        Volatile.Write(ref Stopping, true);
        Volatile.Write(ref ShouldSync, true);
    }
    public static void Run(IEnumerable<string> loop)
    {
        if (Interlocked.CompareExchange(ref schedulerLock, 1,0) != 0)
        {
            throw new Exception("uninitialized scheduler already locked");
        }
        TargetEnumarable = loop;
        
        ScheduleMaker.Build();
        
        
        ThreadCount=CalculateThreadCount();
        
        threads = new WorkerThread[ThreadCount];
        threads[0] = WorkerThread.Create(0,SummonThread:false);
        for (int i = 1; i < ThreadCount; i++)
        {
            threads[i] = WorkerThread.Create(i);
        }
        SyncBarrier = new Barrier(ThreadCount);
        
        Volatile.Write(ref WorkerThreadEnable, true);

        if (!DistributeWork())
        {
            throw new("No work to distribute");
        }
        
        
        // can just be volatile write
        
        if (Interlocked.Exchange(ref schedulerLock, 0) != 1)
        {
            throw new Exception("uninitialized scheduler lock failed");
        }

        threads[0].entryPoint();
        
    }
    
    public static bool TrySchedule()
    {
        if (Interlocked.CompareExchange(ref schedulerLock, 1,0) != 0)
        {
            return false;
        }

        
        
        DistributeWork();
    

        if (Interlocked.Exchange(ref schedulerLock, 0) != 1)
        {
            throw new Exception("uninitialized scheduler lock failed");
        }
        

        return true;
    }
    
    private static unsafe bool DistributeWork()
    {
        // Reselect Procedure
        if (_currentRuntimeScheduleState == null || _currentRuntimeScheduleState.IsCompleted || false)
        { 
            var PreviousSyncMode=RuntimeScheduleState.SyncMode.NA;
            string? PreviousName=null;
            if (_currentRuntimeScheduleState != null)
            {
                PreviousSyncMode = _currentRuntimeScheduleState.syncMode;
                PreviousName = _currentRuntimeScheduleState.TargetName;
            }

            var nextTarget = TMPSELECTOR();
            if (PreviousSyncMode == RuntimeScheduleState.SyncMode.SyncAtEnd || 
                (PreviousName == nextTarget && PreviousSyncMode == RuntimeScheduleState.SyncMode.SyncIfRepeated))
            {
                Volatile.Write(ref ShouldSync,true);
                SpinWait.SpinUntil(()=>SyncBarrier.ParticipantsRemaining == 1);
                TMPSYNCPOINT();
            }
            _currentRuntimeScheduleState = RuntimeScheduleStates[nextTarget];
            _currentRuntimeScheduleState.Reset();
        }
        
        
        //Normal selection Procedure
        var tmpList = __runtimeScheduleItems;
        
        var count = 0;
        for (var i = _currentRuntimeScheduleState.Tail; i < _currentRuntimeScheduleState.Items.Length; i++)
        {
            if (count==20)
            {
                break;
            }
            ref var item = ref _currentRuntimeScheduleState.Items[i];
            
            if (item.IsScheduled) continue;
            
            var canSchedule = true;
            foreach (var dependency in item.Dependencies)
            {
                if (!Volatile.Read(ref _currentRuntimeScheduleState.Items[dependency].IsCompleted))
                {
                    canSchedule = false;
                    break;
                }
            }
            if (!canSchedule) continue;
            for (var j = 0; j < item.Writes.Length; j++)//try acquire write locks
            {
                var resource = item.Writes[j];
                if (Interlocked.CompareExchange(ref resource.state, -1, 0) != 0)
                {
                    canSchedule = false;
                    while (j<0)
                    {
                        j--;
                        var resource2 = item.Writes[j];
                        Volatile.Write(ref resource2.state, 0);
                    }
                    break;
                }
            }
            if (!canSchedule) continue;

            for (int j = 0; j < item.Reads.Length; j++)
            {
                var resource = item.Writes[j];
                var value = Volatile.Read(ref resource.state);
                if (value == -1)
                {//fail unlock all
                    while (j<0)//unlock all reads
                    {
                        j--;
                        var resource2 = item.Writes[j];
                        Interlocked.Decrement(ref resource2.state);
                    }
                    foreach (var t in item.Writes)
                        Volatile.Write(ref t.state, 0);
                    canSchedule = false;
                    break;
                }
                Interlocked.Increment(ref resource.state);
            }
            if (!canSchedule) continue;
            
            item.IsScheduled = true;
            tmpList[count++] = item;
            
            if (i==_currentRuntimeScheduleState.Tail)
            {
                _currentRuntimeScheduleState.Tail++;
                if (i==_currentRuntimeScheduleState.Items.Length-1)
                {
                    _currentRuntimeScheduleState.IsCompleted = true;
                    break;
                }
            }

            if (i>=_currentRuntimeScheduleState.Head)
            {
                _currentRuntimeScheduleState.Head=i;
            }
        }

        if (count == 0)
        {
            return false;
        }
        //divide work among threads
        var threadCount = ThreadCount;
        var workPerThread = count / threadCount;
        var remainder = count % threadCount;
        var start = 0;
        foreach (var thread in threads)
        {
            
            thread.WorkStack.OtherAdd(tmpList[start..(start+workPerThread+(remainder-->0?1:0))]);
        }
        
        //add sanity checks
        if (ShouldSync)
        {
            Volatile.Write(ref ShouldSync,false);
            SyncBarrier.SignalAndWait();
            if (Volatile.Read(ref Stopping))
            {
                return false;
            }
        }
        return true;
    }

    static string TMPSELECTOR()
    {
        currentTargetEnumerator ??= TargetEnumarable.GetEnumerator();
        if (!currentTargetEnumerator.MoveNext())
        {
            currentTargetEnumerator.Dispose();
            currentTargetEnumerator = TargetEnumarable.GetEnumerator();
            currentTargetEnumerator.MoveNext();
        }

        return currentTargetEnumerator.Current;
        
    }
    static void TMPSYNCPOINT()
    {

    }
}

public class ECSScanAttribute:Attribute{}

public class ECSJobAttribute : Attribute
{
    public string[] RunBefore = null!;
    public string[] RunAfter = null!;
    public string Name;
    public ECSResource[] Reads = null!;
    public ECSResource[] Writes = null!;
    
    public ECSJobAttribute(string JobName)
    {
        Name = JobName;
    }
}
