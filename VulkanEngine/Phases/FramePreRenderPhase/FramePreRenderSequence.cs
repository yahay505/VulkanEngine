using VulkanEngine.ECS_internals;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class FramePreRenderSequence
{
    public static void Register()
    {
        // var work = new ExecutionUnitBuilder(CameraControllerJob.CameraControl)
        //     .Named("CameraControl")
        //     .Reads(Input.Input.InputResource)
        //     // .Writes(VKRender.IMGUIResource)
        //     
        //     // .Writes(TransformSystem.Resource,VKRender.IMGUIResource)
        //     
        //     .Build();
        // ScheduleMaker.RegisterToTarget(work, "framePreRender");
    }
}