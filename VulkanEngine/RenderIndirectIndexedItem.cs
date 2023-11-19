using System.Runtime.InteropServices;
using Silk.NET.OpenGL;

namespace VulkanEngine;
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RenderIndirectIndexedItem
{
    uint indexCount;
    uint instanceCount;
    uint firstIndex;
    uint vertexOffset;
    uint firstInstance;
    //extra data
    public float4x4 modelMatrix;


}