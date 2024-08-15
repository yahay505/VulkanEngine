using Vortice.Vulkan;
using VulkanEngine.Renderer.GPUStructs;

namespace VulkanEngine.Renderer;

public struct FrameData
{
    public VkSemaphore
        // ImguiSemaphore, //imgui render finished
        presentSemaphore, //render finished can be presented
        RenderSemaphore, //swapchain render target acquired
        ComputeSemaphore,
        ComputeSemaphore2;
    public VkFence renderFence, computeFence;
    public VkCommandPool commandPool;
    // public CommandBuffer mainCommandBuffer;
    public VkBuffer hostRenderObjectsBuffer;
    public VkDeviceMemory hostRenderObjectsMemory;
    public unsafe void* hostRenderObjectsBufferPtr;
    public unsafe Span<GPUStructs.ComputeInput> hostRenderObjectsBufferAsSpan=>new((void*)
        ((nint) hostRenderObjectsBufferPtr + VKRender.ComputeInSSBOStartOffset),
        hostRenderObjectsBufferSize);
    public unsafe GPUStructs.ComputeInputConfig* computeInputConfig=>(ComputeInputConfig*) hostRenderObjectsBufferPtr;
    public int hostRenderObjectsBufferSize;
    public int hostRenderObjectsBufferSizeInBytes;
    public VkCommandBuffer GfxCommandBuffer, ComputeCommandBuffer;
    public DescriptorSets descriptorSets;

    public VkBuffer uniformBuffer;
    public VkDeviceMemory uniformBufferMemory;
    public unsafe void* uniformBufferMapped;
    
    public struct DescriptorSets
    {
        public VkDescriptorSet GFX;
        public VkDescriptorSet Compute;
    }
}