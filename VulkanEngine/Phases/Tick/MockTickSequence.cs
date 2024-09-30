using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;
using VulkanEngine.Renderer2.infra.Bindless;

namespace VulkanEngine.Phases.Tick;

public static class MockTickSequence
{
    public static void Register()
    {
        var updateMats = new ExecutionUnitBuilder(MockTickSequence.updateMats)
            .Named("update materials")
            .Build();
        ScheduleMaker.RegisterToTarget(updateMats, "tick");

        for(var i=0;i<300;i++)
        {
                
            var work = new ExecutionUnitBuilder(Work)
                .Named("Work")
                // .Writes(VKRender.RendererEcsResource)
                // .Writes(Input.Input.InputResource)
                .RunsAfter(updateMats)
                .Build();
            ScheduleMaker.RegisterToTarget(work, "tick");
        }
    }

    private static unsafe void updateMats()
    {
        // var a = VKRender.CurrentFrame;
        // MaterialManager.SetMaterial(3,new(&a,4));
    }
    private static void Work()
    {
        unsafe
        {

            GetNthFibonacci_Rec(Random.Shared.Next(10));
        }
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