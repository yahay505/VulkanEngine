using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreamblePhase;

public static class PreambleSequence
{
    public static void Register()
    {
        var HandleInputWindow = new ExecutionUnitBuilder(HandleInputAndWindow)
            .Named("HandleInputnWindow")
            .Writes(VKRender.RendererEcsResource) // make more granular
            .Writes(Input.Input.InputResource)
            .Build();
        ScheduleMaker.RegisterToTarget(HandleInputWindow, "frame_preamble");
    }
    private static void HandleInputAndWindow()
    {
        if (VKRender.window!.IsClosing)
        {
            Scheduler.Stop();
        }
        Input.Input.ClearFrameState();
        VKRender.window.DoEvents();
    }
}