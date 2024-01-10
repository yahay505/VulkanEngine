namespace VulkanEngine.ECS_internals.FrameJobs;
[ECSScan]
public class StartTicks
{
    [ECSJob(nameof(AddTicks),RunBefore = new[]{"frame"},RunAfter = new[] {nameof(StartTicks.AddTicks)})]
    public static void AddTicks()
    {
        // Scheduler.SchedulerStack
    }
}