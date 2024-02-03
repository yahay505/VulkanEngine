using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public class VertexBuffer
{
    public VKRender.GPUDynamicBuffer<Vertex> buffer;
    public unsafe VertexBuffer(ulong initialSize)
    {
        buffer = new(initialSize,
            BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.DeviceLocalBit);
    }
    public uint Upload(Span<Vertex> data)
    {
        return buffer.Upload(data, PipelineStageFlags.VertexInputBit);
    }
    public static implicit operator Buffer(VertexBuffer vertexBuffer) => vertexBuffer.buffer.buffer;
    public static implicit operator DeviceMemory(VertexBuffer vertexBuffer) => vertexBuffer.buffer.memory;

}
}