using Vortice.Vulkan;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public class VertexBuffer
{
    public VKRender.GPUDynamicBuffer<DefaultVertex> buffer;
    public unsafe VertexBuffer(ulong initialSize)
    {
        buffer = new(initialSize,
            VkBufferUsageFlags.VertexBuffer,
            VkMemoryPropertyFlags.DeviceLocal,
            "vertex Buffer"u8);
    }
    public uint Upload(Span<DefaultVertex> data)
    {
        return buffer.Upload(data, VkPipelineStageFlags.VertexInput);
    }
    public static implicit operator VkBuffer(VertexBuffer vertexBuffer) => vertexBuffer.buffer.buffer;
    public static implicit operator VkDeviceMemory(VertexBuffer vertexBuffer) => vertexBuffer.buffer.memory;

}
}