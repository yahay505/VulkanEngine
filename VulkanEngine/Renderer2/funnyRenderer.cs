using SixLabors.ImageSharp;
using Vortice.Vulkan;
using VulkanEngine.Renderer2.infra;
using VulkanEngine.Renderer2.infra.Bindless;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer2;

public static class funnyRenderer
{
    private const int INFLIGHT_FRAME = 2;

    private static int frameNo = 0;
    // private static VkSemaphore sempahore;
    private static VkSemaphore[] renderSemaphores;
    private static VkFence[] fences;
    private static EngineImage[] targets = new EngineImage[INFLIGHT_FRAME];
    private static EngineImage[] depthImages=new EngineImage[INFLIGHT_FRAME];

    private static VkFramebuffer[] fbs= new VkFramebuffer[INFLIGHT_FRAME];
    private static VkPipelineLayout pipelineLayout;
    private static VkRenderPass renderpass;
    private static VkCommandPool tpm;
    private static VkPipeline pipeline;
    private static VkDeviceMemory memory;
    private static VkBuffer buff;
    

    public static unsafe void init(EngineWindow window)
    {
        vkCreateCommandPool(API.device, VkCommandPoolCreateFlags.None, API.chosenDevice.indices.graphicsFamily!.Value,
            out tpm);
        for (int i = 0; i < INFLIGHT_FRAME; i++)
        {
            targets[i] = API.CreateImage(window.size.width, window.size.height, VkFormat.R8G8B8A8Srgb, VkImageTiling.Optimal,
                VkImageUsageFlags.ColorAttachment|VkImageUsageFlags.TransferSrc, VkMemoryPropertyFlags.DeviceLocal, false, VkImageCreateFlags.None,
                VkImageAspectFlags.Color);
            API.AllocateDeviceResourcesForImage(targets[i]);
            depthImages[i] = API.CreateImage(window.size.width, window.size.height, API.FindDepthFormat(),
                VkImageTiling.Optimal, VkImageUsageFlags.DepthStencilAttachment, VkMemoryPropertyFlags.DeviceLocal, false,
                VkImageCreateFlags.None, API.HasStencilComponent(API.FindDepthFormat())?VkImageAspectFlags.Stencil | VkImageAspectFlags.Depth: VkImageAspectFlags.Depth);
            API.AllocateDeviceResourcesForImage(depthImages[i]);

        }
        ref var target = ref targets[0];

        renderpass = API.CreateRenderPass([
                new(target.imageFormat, VkSampleCountFlags.Count1, VkAttachmentLoadOp.Clear,
                    VkAttachmentStoreOp.Store,
                    VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store, VkImageLayout.TransferSrcOptimal,
                    VkImageLayout.TransferSrcOptimal),
                new(depthImages[0].imageFormat, VkSampleCountFlags.Count1, VkAttachmentLoadOp.Clear,
                    VkAttachmentStoreOp.Store, VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store,
                    VkImageLayout.DepthStencilAttachmentOptimal, VkImageLayout.DepthStencilAttachmentOptimal)
            ],
            [new()
            {
                srcSubpass = VK_SUBPASS_EXTERNAL,
                dstSubpass = 0,
                dstAccessMask = VkAccessFlags.ColorAttachmentWrite|VkAccessFlags.DepthStencilAttachmentWrite,
                srcAccessMask = VkAccessFlags.TransferRead,
                dstStageMask = VkPipelineStageFlags.AllGraphics,
                srcStageMask = VkPipelineStageFlags.Transfer,
            },
            new()
            {
                srcSubpass = 0,
                dstSubpass = VK_SUBPASS_EXTERNAL,
                srcAccessMask = VkAccessFlags.ColorAttachmentWrite|VkAccessFlags.DepthStencilAttachmentWrite,
                dstAccessMask = VkAccessFlags.TransferRead,
                srcStageMask = VkPipelineStageFlags.AllGraphics,
                dstStageMask = VkPipelineStageFlags.Transfer,
            }],
            [new(0, VkImageLayout.ColorAttachmentOptimal)],
            new(1,VkImageLayout.DepthStencilAttachmentOptimal));
        pipelineLayout = API.CreatePipelineLayout([TextureManager.descSetLayout]);
        var vkPipelineColorBlendAttachmentState = new VkPipelineColorBlendAttachmentState()
        {
            blendEnable = VkBool32.True,
            alphaBlendOp = VkBlendOp.Add,
            srcAlphaBlendFactor = VkBlendFactor.One,
            dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
            srcColorBlendFactor = VkBlendFactor.One,
            dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
            colorBlendOp = VkBlendOp.Add,
            colorWriteMask = VkColorComponentFlags.All,
        };
        var vkPipelineColorBlendStateCreateInfo = new VkPipelineColorBlendStateCreateInfo()
        {
            attachmentCount = 1,
            flags = VkPipelineColorBlendStateCreateFlags.None,
            logicOpEnable = false,
            logicOp = VkLogicOp.Copy,
            pAttachments = &vkPipelineColorBlendAttachmentState,
            pNext = null
        };
        vkPipelineColorBlendStateCreateInfo.blendConstants![0] = 1;
        vkPipelineColorBlendStateCreateInfo.blendConstants![1] = 1;
        vkPipelineColorBlendStateCreateInfo.blendConstants![2] = 1;
        vkPipelineColorBlendStateCreateInfo.blendConstants![3] = 1;
        pipeline= API.CreatePSO(
            [
                new()
                {
                    stage = VkShaderStageFlags.Vertex,
                    module = API.CreateShaderModule(File.ReadAllBytes(API.AssetsPath+"shaders/compiled/Debug/DebugScreenSpaceTri.vert.spv")),
                    pName = (sbyte*) ("main"u8).GetPointer(),
                },
                new()
                {
                    stage = VkShaderStageFlags.Fragment,
                    module = API.CreateShaderModule(File.ReadAllBytes(API.AssetsPath+"shaders/compiled/Debug/DebugtransparentShine.frag.spv")),
                    pName = (sbyte*) ("main"u8).GetPointer(),
                }
            ],
            [new(12), new(8, binding: 1)],
            [new(0, VkFormat.R32G32B32Sfloat, 0, 0), new(1, VkFormat.R32G32Sfloat, 0, 1)],
            VkPrimitiveTopology.TriangleList,
            false,
            [VkDynamicState.Scissor, VkDynamicState.Viewport],
            new()
            {
                lineWidth = 1,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.CounterClockwise,
                polygonMode = VkPolygonMode.Fill,
                
            },
            new(VkSampleCountFlags.Count1),
            new()
            {
                depthTestEnable = true,
                depthWriteEnable = true,
                depthCompareOp = VkCompareOp.Less,
                stencilTestEnable = false,
            },
            vkPipelineColorBlendStateCreateInfo,
            pipelineLayout,
            renderpass, 0
        );
        
        renderSemaphores = new VkSemaphore[INFLIGHT_FRAME];
        for (int i = 0; i < INFLIGHT_FRAME; i++)
            vkCreateSemaphore(API.device, out renderSemaphores[i]);
        fences = new VkFence[INFLIGHT_FRAME];
        for (int i = 0; i < INFLIGHT_FRAME; i++)
            vkCreateFence(API.device,VkFenceCreateFlags.Signaled, out fences[i]);
        for (int i = 0; i < INFLIGHT_FRAME; i++)
            fbs[i]=API.CreateFrameBuffer(target.width,target.height,[targets[i].view,depthImages[i].view],renderpass);

      
        API.CreateBuffer(50,VkBufferUsageFlags.VertexBuffer,VkMemoryPropertyFlags.None, out buff,out memory);

    }

    public static unsafe void Render(EngineWindow window)
    {
        var target_index = frameNo % INFLIGHT_FRAME;
        vkWaitForFences(API.device, fences[target_index], true, ulong.MaxValue);
        vkResetFences(API.device, fences[target_index]);
        ref var target = ref targets[target_index];
        VkCommandBuffer cb;
        vkAllocateCommandBuffer(API.device, tpm, VkCommandBufferLevel.Primary, out cb);//tmp
        vkBeginCommandBuffer(cb, VkCommandBufferUsageFlags.None);
        if (depthImages[target_index].layout[0] != VkImageLayout.DepthStencilAttachmentOptimal)
            API.TransitionImageLayout(cb, depthImages[target_index], VkImageLayout.DepthStencilAttachmentOptimal, 0, 1);
        if (target.layout[0] != VkImageLayout.TransferSrcOptimal)
            API.TransitionImageLayout(cb, target, VkImageLayout.TransferSrcOptimal, 0, 1);
        
        var clears = stackalloc VkClearValue[]{new(0f,.0f,.0f,.0f),new(1f,0)};
        var passInfo = new VkRenderPassBeginInfo()
        {
            renderPass = renderpass,
            clearValueCount = 2,
            framebuffer = fbs[target_index],
            pClearValues = clears,
            renderArea = new(target.width,target.height)
        };
        if (target.width != window.size.width || target.height != window.size.height)
            ;
        vkCmdBeginRenderPass(cb,&passInfo,VkSubpassContents.Inline);

        vkCmdBindPipeline(cb,VkPipelineBindPoint.Graphics,pipeline);
        vkCmdSetViewport(cb,0,new VkViewport(window.size.width,window.size.height));
        vkCmdSetScissor(cb,new(window.size));
        vkCmdBindVertexBuffer(cb,0,buff,0);
        vkCmdBindVertexBuffer(cb,1,buff,0);

        vkCmdDraw(cb,3,1,0,0);
        vkCmdEndRenderPass(cb);

        vkEndCommandBuffer(cb);
        var ss = renderSemaphores[target_index];
        vkQueueSubmit(API.graphicsQueue,
            new VkSubmitInfo()
                {commandBufferCount = 1, pCommandBuffers = &cb, signalSemaphoreCount = 1, pSignalSemaphores = &ss},
            default);
        
        vkAllocateCommandBuffer(API.device, tpm, VkCommandBufferLevel.Primary, out var buf);
        API.present(window,targets[target_index],buf, ss,fences[target_index], () =>
        {
            for (int i = 0; i < INFLIGHT_FRAME; i++)
            {
                var delcopy = targets[i];
                window.CleanupQueue[window.presenterState] += () => API.DestroyImage(delcopy);
                var delcopyD = depthImages[i];
                window.CleanupQueue[window.presenterState] += () => API.DestroyImage(delcopyD);

                targets[i] = API.CreateImage(window.size.width, window.size.height, VkFormat.R8G8B8A8Srgb,
                    VkImageTiling.Optimal,
                    VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
                    VkMemoryPropertyFlags.DeviceLocal, false, VkImageCreateFlags.None,
                    VkImageAspectFlags.Color);
                API.AllocateDeviceResourcesForImage(targets[i]);
                depthImages[i] = API.CreateImage(window.size.width, window.size.height, API.FindDepthFormat(),
                    VkImageTiling.Optimal, VkImageUsageFlags.DepthStencilAttachment, VkMemoryPropertyFlags.DeviceLocal,
                    false,
                    VkImageCreateFlags.None,
                    API.HasStencilComponent(API.FindDepthFormat())
                        ? VkImageAspectFlags.Stencil | VkImageAspectFlags.Depth
                        : VkImageAspectFlags.Depth);
                API.AllocateDeviceResourcesForImage(depthImages[i]);
                var delcopy_fb = fbs[i];
                window.CleanupQueue[window.presenterState] += () => vkDestroyFramebuffer(API.device, delcopy_fb);
                fbs[i]=API.CreateFrameBuffer(targets[i].width,targets[i].height,[targets[i].view,depthImages[i].view],renderpass);

            }
        });
        frameNo++;
    }


}