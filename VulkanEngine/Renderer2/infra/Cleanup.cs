using Vortice.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static class Cleanup
{
    private static Dictionary<nuint, Action> db = new();

    public static void RegisterForCleanupAfterCBCompletion<T>(VkCommandBuffer cb, T target) where T : unmanaged
    {
        db.TryAdd((nuint) cb.Handle, () => { });
        db[(nuint) cb.Handle] += target switch
        {
            int _ => () => { },
            _ => throw new Exception()
        };
    }

    public static void CbCompleted(VkCommandBuffer cb)
    {
        db[(nuint) cb.Handle]();
        db.Remove((nuint) cb.Handle);
    }
}