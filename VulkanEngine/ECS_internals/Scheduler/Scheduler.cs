using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public static class Scheduler
{
    static List<string> SchedulerTargets = new(){"tick","frame"};
    static Stack<string> SchedulerStack = new();
    private static Dictionary<string, List<string>> staticTargetWantedBy = new();
    private static Dictionary<string, List<string>> dynamicTargetDependsOn = new();
    static bool sync=false;
    static Scheduler()
    {
        // CollectViaReflection();
    }
    public static void InitSync()
    {
        sync = true;
    }
    public static void InitMultithreaded()
    {
        throw new NotImplementedException();
    }
    public static void RunOnce(string target)
    {

    }

    public static void RunToEndSync()
    {
        
    }
    
    private static void RunSingleThreaded()
    {
        
    }
    private static void RunMultiThreaded()
    {
        throw new NotImplementedException();
    }
    public static unsafe void RegisterRecurrent<T>(Delegate a)
    {
        
    }
    


}
public class ScheduleItem
{
    public string Name;
    public ScheduleItem[] RunBefore = null!;
    public ScheduleItem[] RunAfter = null!;
    public IResource[] Reads = null!;
    public IResource[] Writes = null!;
    public Delegate Function = null!;
}
public class ECSScanAttribute:Attribute{}

public class ECSJobAttribute : Attribute
{
    public string[] RunBefore = null!;
    public string[] RunAfter = null!;
    public string Name;
    public IResource[] Reads = null!;
    public IResource[] Writes = null!;
    
    public ECSJobAttribute(string JobName)
    {
        Name = JobName;
    }
}
