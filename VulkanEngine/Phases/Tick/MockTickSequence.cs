using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.Tick;

public static class MockTickSequence
{
    public static void Register()
    {
        for(var i=0;i<30;i++)
        {
            var work = new ExecutionUnitBuilder(Work)
                .Named("Work")
                // .Writes(VKRender.RendererEcsResource)
                // .Writes(Input.Input.InputResource)
                .Build();
            ScheduleMaker.RegisterToTarget(work, "tick");
        }
    }
    private static void Work()
    {
        GetNthFibonacci_Rec(Random.Shared.Next(30));
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