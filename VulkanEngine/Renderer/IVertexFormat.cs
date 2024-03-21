using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public interface IVertexFormat
{
    public static abstract VertexInputAttributeDescription[] GetAttributeDescriptions();

}