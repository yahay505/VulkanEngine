using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using VulkanEngine.Renderer2.infra;

namespace VulkanEngine.Renderer2.Text;

public struct TextVertexFormat:IVertexFormat
{
    public float2 pos;
    public float2 uv;
    public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(int bindNo)
    {
        var attributeDescriptions = new[]
        {
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 0,
                format = VkFormat.R32G32Sfloat,
                offset = 0,
            },
            new VkVertexInputAttributeDescription()
            {
                binding = 1,
                location = 1,
                format = VkFormat.R32G32Sfloat,
                offset = 0,
            },
        };

        return attributeDescriptions;
        
    }

    public static VkVertexInputBindingDescription[] GetBindingDescription(int baseBindNo)
    {
        var bindingDescription = new[]
        {
            new VkVertexInputBindingDescription()
            {
                binding = (uint)baseBindNo,
                stride = (uint)Unsafe.SizeOf<float2>(),
                inputRate = VkVertexInputRate.Vertex,
            },
            new VkVertexInputBindingDescription()
            {
                binding = (uint)baseBindNo+1,
                stride = (uint)Unsafe.SizeOf<float2>(),
                inputRate = VkVertexInputRate.Vertex,
            }
        };

        return bindingDescription;
    }
    public static implicit operator TextVertexFormat((float2 pos, float2 uv) d)
    {
        return new TextVertexFormat
        {
            pos = d.pos,
            uv = d.uv
        };
    }
    
}