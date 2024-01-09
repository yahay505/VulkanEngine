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
        CollectViaReflection();
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
        if (!SchedulerTargets.Contains(target))
            throw new ArgumentException("target must be one of the following: "+string.Join(", ",SchedulerTargets));
        
        SchedulerStack.Push(target);
        // Run();
        SchedulerStack.Pop();
        //remap
        if (sync)
            RunSingleThreaded();
        else
            RunMultiThreaded();
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
    
    private static readonly AssemblyBuilder _assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("DynamicMethodBuilderAssembly"), AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder _moduleBuilder = _assemblyBuilder.DefineDynamicModule("DynamicModule");
    public static unsafe void CollectViaReflection()
    {

        var nameWithoutCollision = $"ECS_Call_Proxy_{Guid.NewGuid():N}";
        var typeBuilder = _moduleBuilder.DefineType(nameWithoutCollision, TypeAttributes.Public | TypeAttributes.Class);

        //get all types in all loaded assemblies
        var a = AppDomain.CurrentDomain.GetAssemblies().
            Where(ass=>!ass.IsDynamic)
            .SelectMany(ass => ass.GetTypes())
            .Where(t => t.CustomAttributes.Any(z=>z.AttributeType==typeof(ECSScanAttribute)))
            .SelectMany(t=>t.GetMethods())
            .Where(m=>m.CustomAttributes.Any(z=>z.AttributeType==typeof(ECSJobAttribute)))
            .Select(m=>(m,(ECSJobAttribute)(Attribute.GetCustomAttribute(m,typeof(ECSJobAttribute))!)));
            
            
            ;
        var nameList = new List<string>();
        foreach (var (method,attr) in a)
        {
            var name = $"proxy_{method.Name}";
            nameList.Add(name);
            var proxymet = typeBuilder.DefineMethod(
                name,
                MethodAttributes.Public | MethodAttributes.Static,
                null,
                new[] {typeof(void*)}
            );
            {
                var il = proxymet.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Tailcall);
                il.EmitCall(OpCodes.Call, method, null);
                il.Emit(OpCodes.Ret);
            }
        }
        var t = typeBuilder.CreateType();
        var fns = nameList.Select(z=>t.GetMethod(z)!.MethodHandle.GetFunctionPointer()).ToList();

        var q = new ECSQuery<Transform_ref, MeshData>();

#pragma warning disable CS8500
        ((delegate* managed<void*, void>) fns[0])(&q);
#pragma warning restore CS8500
    }


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
