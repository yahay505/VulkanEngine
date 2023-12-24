using System.Diagnostics;

namespace VulkanEngine.ECS_internals;

public static class Scheduler
{
    static bool sync=false;
    public static void InitSync()
    {
        sync = true;
    }
    public static void InitMultithreaded()
    {
        throw new NotImplementedException();
    }
    public static void Run()
    {
        //remap
        throw new NotImplementedException();
    }

    public static unsafe void RegisterRecurrent<T>(Delegate a)
    {
    }
}

public class Task
{
    Task[] depends_on;
    Task[] wanted_by;
    unsafe delegate* managed<void> function;
}