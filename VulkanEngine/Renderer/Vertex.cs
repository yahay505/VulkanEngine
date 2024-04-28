using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Vortice.Vulkan;

namespace VulkanEngine.Renderer;
[StructLayout( LayoutKind.Sequential)]
public struct Vertex:IVertexFormat
{
    public Vector3D<float> pos;//12
    public Vector3D<float> color;//12
    public Vector2D<float> texCoord;//8

    public static implicit operator Vertex(((float x, float y,float z) pos,(float r,float g,float b) color,(float u,float w) texCoord) d)
    {
        return new Vertex
        {
            pos = new Vector3D<float>(d.pos.x, d.pos.y,d.pos.z),
            color = new Vector3D<float>(d.color.r, d.color.g, d.color.b),
            texCoord = new Vector2D<float>(d.texCoord.u, d.texCoord.w)
        };
    }
    public Vertex()
    {
    }

    public Vertex(Vector3D<float> pos, Vector3D<float> color, Vector2D<float> texCoord)
    {
        this.pos = pos;
        this.color = color;
        this.texCoord = texCoord;
    }

    public static VkVertexInputBindingDescription GetBindingDescription()
    {
        VkVertexInputBindingDescription bindingDescription = new()
        {
            binding = 0,
            stride = (uint)Unsafe.SizeOf<Vertex>(),
            inputRate = VkVertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VkVertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 0,
                format = VkFormat.R32G32B32Sfloat,
                offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 1,
                format = VkFormat.R32G32B32Sfloat,
                offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VkVertexInputAttributeDescription()
            {
                binding = 0,
                location = 2,
                format = VkFormat.R32G32Sfloat,
                offset = (uint)Marshal.OffsetOf<Vertex>(nameof(texCoord)),
            },
        };

         return attributeDescriptions;
    }
}