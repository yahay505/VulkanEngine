using Vortice.Vulkan;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public class IndexBuffer
{
    VKRender.GPUDynamicBuffer<uint> buffer;
    public unsafe IndexBuffer(ulong initialSize)
    {
        buffer = new(initialSize,
            VkBufferUsageFlags.IndexBuffer,
            VkMemoryPropertyFlags.DeviceLocal,
            "indexBuffer"u8);
        
    }
    public uint Upload(Span<uint> data)
    {
        return buffer.Upload(data, VkPipelineStageFlags.VertexInput);
    }
    //implicit cast
    public static implicit operator VkBuffer(IndexBuffer indexBuffer) => indexBuffer.buffer.buffer;
    public static implicit operator VkDeviceMemory(IndexBuffer indexBuffer) => indexBuffer.buffer.memory;
    
}
}