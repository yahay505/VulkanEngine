namespace VulkanEngine.Renderer2.infra.Config;

public static class GPUBindingPoints
{
    public const int GPU_Compute_Input_Data = 1;
    public const int GPU_Compute_Output_Data = 2;
    public const int GPU_Compute_Input_Mesh = 3;
    public const int GPU_Compute_Input_Materials = 4;
    // public const int GPU_Compute_Output_Secondary = 4;
    public const int GPU_Gfx_UBO = 0;
    public const int GPU_Gfx_Image_Sampler = 1;
    public const int GPU_Gfx_Input_Indirect = 2;
}