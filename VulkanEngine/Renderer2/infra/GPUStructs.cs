using System.Runtime.InteropServices;

namespace VulkanEngine.Renderer2.infra;


[StructLayout(LayoutKind.Sequential, Size = 5*4, Pack = 1)]
public struct VkDrawIndexedIndirectCommand {
    uint    indexCount;
    uint    instanceCount;
    uint    firstIndex;
    int     vertexOffset;
    uint    firstInstance;
}