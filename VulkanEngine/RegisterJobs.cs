using System.Diagnostics;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine;

public static class RegisterJobs
{
    static RegisterJobs()
    {
        for (int i = 0; i < 8; i++)
        {
            var renderUnit = new ExecutionUnitBuilder(LoadTest)
                .Named($"LOAD{i}")
                .Build();
            ScheduleMaker.RegisterToTarget(renderUnit, "LOADTEST");
        }
    }
    public static void LoadTest()
    {
        var stopw = Stopwatch.StartNew();
        while (stopw.ElapsedMilliseconds<2) { }
    }
}