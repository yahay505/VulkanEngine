using Vortice.Vulkan;

namespace VulkanEngine.Renderer;

public interface IVertexFormat
{
    public static abstract VkVertexInputAttributeDescription[] GetAttributeDescriptions();

}