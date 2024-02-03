using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public class IndexBuffer
{
    VKRender.GPUDynamicBuffer<uint> buffer;
    public unsafe IndexBuffer(ulong initialSize)
    {
        buffer = new(initialSize,
            BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit);
        
    }
    public uint Upload(Span<uint> data)
    {
        return buffer.Upload(data, PipelineStageFlags.VertexInputBit);
    }
    //implicit cast
    public static implicit operator Buffer(IndexBuffer indexBuffer) => indexBuffer.buffer.buffer;
    public static implicit operator DeviceMemory(IndexBuffer indexBuffer) => indexBuffer.buffer.memory;
    
}
}