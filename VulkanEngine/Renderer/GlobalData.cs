using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static class GlobalData
    {
        public static Image depthImage;
        public static Format depthFormat;
        public static DeviceMemory depthImageMemory;
        public static ImageView depthImageView;

        public static CommandPool globalCommandPool;
        public static CommandBuffer oneTimeUseCommandBuffer;
        
        public static Buffer deviceRenderObjectsBuffer;
        public static DeviceMemory deviceRenderObjectsMemory;
        public static int deviceRenderObjectsBufferSize;
        public static int deviceRenderObjectsBufferSizeInBytes;
        public static unsafe void* DEBUG_deviceRenderObjectsBufferPtr;
        public static unsafe GPUStructs.ComputeInputConfig* DEBUG_deviceRenderObjectsBufferCONFIG =>
            (GPUStructs.ComputeInputConfig*) DEBUG_deviceRenderObjectsBufferPtr;
        public static unsafe Span<GPUStructs.ComputeInput> DEBUG_deviceRenderObjectsBufferDATAAsSpan=>new((void*)
            ((UIntPtr) DEBUG_deviceRenderObjectsBufferPtr+ComputeInSSBOStartOffset), deviceRenderObjectsBufferSize);

        public static Buffer deviceIndirectDrawBuffer;
        public static DeviceMemory deviceIndirectDrawBufferMemory;
        public static int deviceIndirectDrawBufferSize;
        public static int deviceIndirectDrawBufferSizeInBytes;
        public static unsafe void* DEBUG_deviceIndirectDrawBufferPtr;

        public static unsafe GPUStructs.ComputeOutputConfig* DEBUG_deviceIndirectDrawBufferCONFIG =>
            (GPUStructs.ComputeOutputConfig*) DEBUG_deviceIndirectDrawBufferPtr;
        public static unsafe Span<GPUStructs.ComputeDrawOutput> DEBUG_deviceIndirectDrawBufferDATAAsSpan=>new((void*)
            ((UIntPtr) DEBUG_deviceIndirectDrawBufferPtr+ComputeOutSSBOStartOffset), deviceIndirectDrawBufferSize);

        public static Buffer MeshInfoBuffer;
        public static DeviceMemory MeshInfoBufferMemory;
        public static int MeshInfoBufferSize;
        public static unsafe void* MeshInfoBufferPtr;
        public static unsafe Span<GPUStructs.MeshInfo> DEBUG_MeshInfoBufferDATAAsSpan=>new((void*)MeshInfoBufferPtr, MeshInfoBufferSize);
        
        public static DeviceMemory ReadBackMemory;
        public static Buffer ReadBackBuffer;
        public static unsafe void* ReadBackBufferPtr;
        
        internal static VertexBuffer vertexBuffer;
        public static IndexBuffer indexBuffer;
    }

    private static unsafe void AllocateGlobalData()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _familyIndices.graphicsFamily!.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };
        vk.CreateCommandPool(device, &poolInfo, null, out GlobalData.globalCommandPool)
            .Expect("failed to create command pool!");
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = GlobalData.globalCommandPool,
            CommandBufferCount = 1
        };
        fixed(CommandBuffer* cmd = &GlobalData.oneTimeUseCommandBuffer)
            vk.AllocateCommandBuffers(device, &allocInfo, cmd)
                .Expect("failed to allocate command buffer!");
        GlobalData.indexBuffer = new IndexBuffer(1);
        GlobalData.vertexBuffer = new VertexBuffer(1);
    }
    private static unsafe void FreeGlobalData()
    {
        vk.DestroyCommandPool(device, GlobalData.globalCommandPool, null);
    }
}