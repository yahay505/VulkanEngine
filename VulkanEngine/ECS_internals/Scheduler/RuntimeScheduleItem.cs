using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public struct RuntimeScheduleItem
{
    public bool IsScheduled;
    public bool IsCompleted;
    public int[] Dependencies;
    public ECSResource[] Reads;
    public ECSResource[] Writes;
}