using Silk.NET.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine.Renderer;

public struct FrameData
{
    public Semaphore
        // ImguiSemaphore, //imgui render finished
        presentSemaphore, //render finished can be presented
        RenderSemaphore, //swapchain render target acquired
        ComputeSemaphore,
        ComputeSemaphore2;
    public Fence renderFence, computeFence;
    public CommandPool commandPool;
    // public CommandBuffer mainCommandBuffer;
    public Buffer stagingBuffer;
    public DeviceMemory stagingMemory;
    public Buffer hostRenderObjectsBuffer;
    public DeviceMemory hostRenderObjectsMemory;
    public unsafe void* hostRenderObjectsBufferPtr;
    public unsafe Span<GPUStructs.ComputeInput> hostRenderObjectsBufferAsSpan=>new((void*)
        ((nint) hostRenderObjectsBufferPtr + VKRender.ComputeInSSBOStartOffset),
        hostRenderObjectsBufferSize);
    public unsafe GPUStructs.ComputeInputConfig* computeInputConfig=>(ComputeInputConfig*) hostRenderObjectsBufferPtr;
    public int hostRenderObjectsBufferSize;
    public int hostRenderObjectsBufferSizeInBytes;
    public CommandBuffer GfxCommandBuffer, ComputeCommandBuffer;
    public DescriptorSets descriptorSets;

    public Buffer uniformBuffer;
    public DeviceMemory uniformBufferMemory;
    public unsafe void* uniformBufferMapped;
    
    public struct DescriptorSets
    {
        public DescriptorSet GFX, Compute;
    }
}