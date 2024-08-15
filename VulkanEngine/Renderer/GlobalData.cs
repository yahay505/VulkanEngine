using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static class GlobalData
    {
        // public static Image depthImage;
        // public static Format depthFormat;
        // public static VkDeviceMemory depthImageMemory;
        // public static ImageView depthImageView;

        public static VkCommandPool globalCommandPool;
        public static VkCommandBuffer oneTimeUseCommandBuffer;
        public static VkFence oneTimeUseCBFence;

        
        public static VkBuffer deviceRenderObjectsBuffer;
        public static VkDeviceMemory deviceRenderObjectsMemory;
        public static int deviceRenderObjectsBufferSize;
        public static int deviceRenderObjectsBufferSizeInBytes;
        public static unsafe void* DEBUG_deviceRenderObjectsBufferPtr;
        public static unsafe GPUStructs.ComputeInputConfig* DEBUG_deviceRenderObjectsBufferCONFIG =>
            (GPUStructs.ComputeInputConfig*) DEBUG_deviceRenderObjectsBufferPtr;
        public static unsafe Span<GPUStructs.ComputeInput> DEBUG_deviceRenderObjectsBufferDATAAsSpan=>new((void*)
            ((nuint) DEBUG_deviceRenderObjectsBufferPtr+ComputeInSSBOStartOffset), deviceRenderObjectsBufferSize);

        public static VkBuffer deviceIndirectDrawBuffer;
        public static VkDeviceMemory deviceIndirectDrawBufferMemory;
        public static int deviceIndirectDrawBufferSize;
        public static int deviceIndirectDrawBufferSizeInBytes;
        public static unsafe void* DEBUG_deviceIndirectDrawBufferPtr;

        public static unsafe GPUStructs.ComputeOutputConfig* DEBUG_deviceIndirectDrawBufferCONFIG =>
            (GPUStructs.ComputeOutputConfig*) DEBUG_deviceIndirectDrawBufferPtr;
        public static unsafe Span<GPUStructs.ComputeDrawOutput> DEBUG_deviceIndirectDrawBufferDATAAsSpan=>new((void*)
            ((UIntPtr) DEBUG_deviceIndirectDrawBufferPtr+ComputeOutSSBOStartOffset), deviceIndirectDrawBufferSize);

        public static VkBuffer MeshInfoBuffer;
        public static VkDeviceMemory MeshInfoBufferMemory;
        public static int MeshInfoBufferSize;
        public static unsafe void* MeshInfoBufferPtr;
        public static unsafe Span<GPUStructs.MeshInfo> DEBUG_MeshInfoBufferDATAAsSpan=>new((void*)MeshInfoBufferPtr, MeshInfoBufferSize);
        
        public static VkDeviceMemory ReadBackMemory;
        public static VkBuffer ReadBackBuffer;
        public static unsafe void* ReadBackBufferPtr;
        
        internal static VertexBuffer vertexBuffer;
        public static IndexBuffer indexBuffer;
    }

    private static unsafe void AllocateGlobalData()
    {
        VkCommandPoolCreateInfo poolInfo = new()
       {
            queueFamilyIndex = _familyIndices.graphicsFamily!.Value,
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer
        };
        vkCreateCommandPool(device, &poolInfo, null, out GlobalData.globalCommandPool)
            .Expect("failed to create command pool!");
        VkCommandBufferAllocateInfo allocInfo = new()
       {
            level = VkCommandBufferLevel.Primary,
            commandPool = GlobalData.globalCommandPool,
            commandBufferCount = 1
        };
        fixed(VkCommandBuffer* cmd = &GlobalData.oneTimeUseCommandBuffer)
            vkAllocateCommandBuffers(device, &allocInfo, cmd)
                .Expect("failed to allocate command buffer!");
        GlobalData.indexBuffer = new IndexBuffer(1);
        GlobalData.vertexBuffer = new VertexBuffer(1);
        vkCreateFence(device, out GlobalData.oneTimeUseCBFence)
            .Expect();
    }
    private static unsafe void FreeGlobalData()
    {
        vkDestroyCommandPool(device, GlobalData.globalCommandPool, null);
        vkDestroyFence(device, GlobalData.oneTimeUseCBFence);
    }
}