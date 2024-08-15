using System.Runtime.InteropServices;

namespace VulkanEngine.Renderer.GPUStructs;

[StructLayout(LayoutKind.Explicit,Size = 128, Pack = 1)]
public struct ComputeInput{
    [FieldOffset(0)]
    public float4x4 transform;
    [FieldOffset(64)]
    public uint meshID;
    [FieldOffset(68)]
    public uint materialID;
    [FieldOffset(72)]
    unsafe fixed int padding[14];
};

[StructLayout(LayoutKind.Sequential, Size = 5*4, Pack = 1)]
public struct VkDrawIndexedIndirectCommand {
    uint    indexCount;
    uint    instanceCount;
    uint    firstIndex;
    int     vertexOffset;
    uint    firstInstance;
};
[StructLayout(LayoutKind.Sequential, Size = 128, Pack = 1)]
public struct ComputeDrawOutput{
    VkDrawIndexedIndirectCommand command;
    uint materialID;
    unsafe fixed int padding[10];
    float4x4 model;
};
[StructLayout(LayoutKind.Sequential, Size = 64, Pack = 1)]
public struct ComputeInputConfig{
    public uint objectCount;
    
    unsafe fixed int padding[15];
};
[StructLayout(LayoutKind.Sequential, Size = (int)VKRender.ComputeOutSSBOStartOffset, Pack = 1)]
public struct ComputeOutputConfig{
    uint objectCount;
    unsafe fixed int padding[15];
};
[StructLayout(LayoutKind.Sequential, Size = 16, Pack = 1)]
public struct MeshInfo{
    public uint IBOoffset;
    public uint IBOsize;
    public uint vertexLoadOffset;
    public int padding;
};
