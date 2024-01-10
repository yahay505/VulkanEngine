using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals.FrameJobs;
[ECSScan]
public static class ImguiJob
{
    // [ECSJob(nameof(RenderImgui),RunBefore = "frame",RunAfter = new[] {nameof(StartTicks.AddTicks)},Writes = ImGuiResource)]
    public static void RenderImgui()
    {
        aaaa(RenderImgui);
    }

    public static void aaaa<T>(T a) where T:Delegate
    {
        Console.WriteLine(typeof(T)+" "+ a.Method.Name + " " + a.Method.GetType().FullName);
    }
}