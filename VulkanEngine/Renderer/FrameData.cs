using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine.Renderer;

public struct FrameData
{
    public Semaphore
        ImguiSemaphore,//imgui render finished
        presentSemaphore,//render finished can be presented
        RenderSemaphore,//swapchain render target acquired
        transferSemaphore;//transfer finished
    public Fence renderFence;
    public CommandPool commandPool;
    public CommandBuffer mainCommandBuffer;
    public Buffer stagingBuffer;
    public DeviceMemory stagingBufferMemory;
    
}