using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public class RuntimeScheduleItem
{
    public string Name;
    public bool IsScheduled;
    public bool IsCompleted;
    public int[] Dependencies;
    public ECSResource[] Reads;
    public ECSResource[] Writes;
    public unsafe delegate* managed<void> Function;
}