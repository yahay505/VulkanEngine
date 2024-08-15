using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Vortice.Vulkan;

namespace VulkanEngine.Renderer;
[StructLayout( LayoutKind.Sequential)]
public struct DefaultVertex:IVertexFormat
{
    public Vector3D<float> pos;//12
    public Vector3D<float> color;//12
    public Vector2D<float> texCoord;//8

    public static implicit operator DefaultVertex(((float x, float y,float z) pos,(float r,float g,float b) color,(float u,float w) texCoord) d)
    {
        return new DefaultVertex
        {
            pos = new Vector3D<float>(d.pos.x, d.pos.y,d.pos.z),
            color = new Vector3D<float>(d.color.r, d.color.g, d.color.b),
            texCoord = new Vector2D<float>(d.texCoord.u, d.texCoord.w)
        };
    }
    public DefaultVertex()
    {
    }

    public DefaultVertex(Vector3D<float> pos, Vector3D<float> color, Vector2D<float> texCoord)
    {
        this.pos = pos;
        this.color = color;
        this.texCoord = texCoord;
    }

    public static VkVertexInputBindingDescription[] GetBindingDescription(int bindNo)
    {
        var bindingDescription = new[]
        {
            new VkVertexInputBindingDescription()
            {
                binding = (uint)bindNo,
                stride = (uint)Unsafe.SizeOf<DefaultVertex>(),
                inputRate = VkVertexInputRate.Vertex,
            }
        };

        return bindingDescription;
    }

    public static VkVertexInputAttributeDescription[] GetAttributeDescriptions(int bindNo)
    {
        var attributeDescriptions = new[]
        {
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 0,
                format = VkFormat.R32G32B32Sfloat,
                offset = (uint)Marshal.OffsetOf<DefaultVertex>(nameof(pos)),
            },
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 1,
                format = VkFormat.R32G32B32Sfloat,
                offset = (uint)Marshal.OffsetOf<DefaultVertex>(nameof(color)),
            },
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 2,
                format = VkFormat.R32G32Sfloat,
                offset = (uint)Marshal.OffsetOf<DefaultVertex>(nameof(texCoord)),
            },
        };

         return attributeDescriptions;
    }
}