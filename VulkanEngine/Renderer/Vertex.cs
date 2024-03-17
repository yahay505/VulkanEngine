using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;
[StructLayout( LayoutKind.Sequential)]
public struct Vertex
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

    public static VertexInputBindingDescription GetBindingDescription()
    {
        VertexInputBindingDescription bindingDescription = new()
        {
            Binding = 0,
            Stride = (uint)Unsafe.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex,
        };

        return bindingDescription;
    }

    public static VertexInputAttributeDescription[] GetAttributeDescriptions()
    {
        var attributeDescriptions = new[]
        {
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 0,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 1,
                Format = Format.R32G32B32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            },
            new VertexInputAttributeDescription()
            {
                Binding = 0,
                Location = 2,
                Format = Format.R32G32Sfloat,
                Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(texCoord)),
            },
        };

         return attributeDescriptions;
    }
}