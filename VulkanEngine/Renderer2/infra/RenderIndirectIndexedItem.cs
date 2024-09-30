using System.Runtime.InteropServices;

namespace VulkanEngine.Renderer2.infra;
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RenderIndirectIndexedItem
{
    uint indexCount;
    uint instanceCount;
    uint firstIndex;
    int vertexOffset;
    uint firstInstance;
    //extra data
    public float4x4 modelMatrix;


}