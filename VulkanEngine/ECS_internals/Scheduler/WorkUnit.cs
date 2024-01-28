namespace VulkanEngine.ECS_internals;

public struct WorkUnit
{
    public unsafe delegate* managed<void> Function;
    public unsafe RuntimeScheduleItem* Item;
}