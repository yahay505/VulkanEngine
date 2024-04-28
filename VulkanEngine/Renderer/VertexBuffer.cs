using Vortice.Vulkan;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public class VertexBuffer
{
    public VKRender.GPUDynamicBuffer<Vertex> buffer;
    public unsafe VertexBuffer(ulong initialSize)
    {
        buffer = new(initialSize,
            VkBufferUsageFlags.VertexBuffer,
            VkMemoryPropertyFlags.DeviceLocal);
    }
    public uint Upload(Span<Vertex> data)
    {
        return buffer.Upload(data, VkPipelineStageFlags.VertexInput);
    }
    public static implicit operator VkBuffer(VertexBuffer vertexBuffer) => vertexBuffer.buffer.buffer;
    public static implicit operator VkDeviceMemory(VertexBuffer vertexBuffer) => vertexBuffer.buffer.memory;

}
}