using Vortice.Vulkan;

namespace VulkanEngine.Renderer;

public interface IVertexFormat
{
    public static abstract VkVertexInputAttributeDescription[] GetAttributeDescriptions(int bindNo);
    public static abstract VkVertexInputBindingDescription[] GetBindingDescription(int bindNo);

}