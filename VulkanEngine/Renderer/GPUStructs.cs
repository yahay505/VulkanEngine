using System.Runtime.InteropServices;

namespace VulkanEngine.Renderer.GPUStructs;

public static class BindingPoints
{
    public const int GPU_Compute_Input_Data = 1;
    public const int GPU_Compute_Output_Data = 2;
    public const int GPU_Compute_Input_Mesh = 3;
    // public const int GPU_Compute_Output_Secondary = 4;
    public const int GPU_Gfx_UBO = 0;
    public const int GPU_Gfx_Image_Sampler = 1;
    public const int GPU_Gfx_Input_Indirect = 2;
    

}


[StructLayout(LayoutKind.Explicit, Size = 128, Pack = 1)]
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
public struct ComputeOutput{
    VkDrawIndexedIndirectCommand command;
    uint materialID;
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
};
