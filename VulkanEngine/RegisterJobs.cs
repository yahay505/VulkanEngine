using VulkanEngine.ECS_internals;

namespace VulkanEngine;

public static class RegisterJobs
{
    static RegisterJobs()
    {
        for (int i = 0; i < 30; i++)
        {
            var renderUnit = new ExecutionUnitBuilder(LoadTest)
                .Named($"LOAD{i}")
                .Build();
            ScheduleMaker.RegisterToTarget(renderUnit, "LOADTEST");
        }
    }
    public static void LoadTest()
    {
        GetNthFibonacci_Rec(40);
    }
    
    public static int GetNthFibonacci_Rec(int n)
    {
        if ((n == 0) || (n == 1))
        {
            return n;
        }
        else
            return GetNthFibonacci_Rec(n - 1) + GetNthFibonacci_Rec(n - 2);
    }
}
