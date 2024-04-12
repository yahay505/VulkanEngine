using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreamblePhase;

public static class PreambleSequence
{
    public static void Register()
    {
        var HandleInputWindow = new ExecutionUnitBuilder(HandleInputAndWindow)
            .Named("HandleInputnWindow")
            .Writes(VKRender.RendererEcsResource,Input.Input.InputResource) // make more granular
            .Build();
        ScheduleMaker.RegisterToTarget(HandleInputWindow, "frame_preamble");
    }
    private static void HandleInputAndWindow()
    {
        if (VKRender.mainWindow.window!.IsClosing&&!Volatile.Read(ref Scheduler.Stopping))
        {
            Scheduler.Stop();
        }
        Input.Input.Update();
    }
}