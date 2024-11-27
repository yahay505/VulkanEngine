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
    private static EngineImage target;
    private static VkFramebuffer fb;
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
        target = API.CreateImage(window.size.width, window.size.height, VkFormat.R8G8B8A8Srgb, VkImageTiling.Optimal,
            VkImageUsageFlags.ColorAttachment|VkImageUsageFlags.TransferSrc, VkMemoryPropertyFlags.DeviceLocal, false, VkImageCreateFlags.None,
            VkImageAspectFlags.Color);
        API.AllocateDeviceResourcesForImage(target);
        window.depthImage = API.CreateImage(window.size.width, window.size.height, API.FindDepthFormat(),
            VkImageTiling.Optimal, VkImageUsageFlags.DepthStencilAttachment, VkMemoryPropertyFlags.DeviceLocal, false,
            VkImageCreateFlags.None, API.HasStencilComponent(API.FindDepthFormat())?VkImageAspectFlags.Stencil | VkImageAspectFlags.Depth: VkImageAspectFlags.Depth);
        API.AllocateDeviceResourcesForImage(window.depthImage);
        renderpass = API.CreateRenderPass([
                new(target.imageFormat, VkSampleCountFlags.Count1, VkAttachmentLoadOp.Clear,
                    VkAttachmentStoreOp.Store,
                    VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store, VkImageLayout.TransferSrcOptimal,
                    VkImageLayout.TransferSrcOptimal),
                new(window.depthImage.imageFormat, VkSampleCountFlags.Count1, VkAttachmentLoadOp.Clear,
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
        fb = API.CreateFrameBuffer(target.width,target.height,[target.view,window.depthImage.view],renderpass);
      
        API.CreateBuffer(50,VkBufferUsageFlags.VertexBuffer,VkMemoryPropertyFlags.None, out buff,out memory);

    }

    public static unsafe void Render(EngineWindow window)
    {
        vkWaitForFences(API.device, fences[frameNo % INFLIGHT_FRAME], true, ulong.MaxValue);
        vkResetFences(API.device, fences[frameNo % INFLIGHT_FRAME]);
        VkCommandBuffer cb;
        vkAllocateCommandBuffer(API.device, tpm, VkCommandBufferLevel.Primary, out cb);//tmp
        vkBeginCommandBuffer(cb, VkCommandBufferUsageFlags.None);
        if (window.depthImage.layout[0] != VkImageLayout.DepthStencilAttachmentOptimal)
            API.TransitionImageLayout(cb, window.depthImage, VkImageLayout.DepthStencilAttachmentOptimal, 0, 1);
        if (target.layout[0] != VkImageLayout.TransferSrcOptimal)
            API.TransitionImageLayout(cb, target, VkImageLayout.TransferSrcOptimal, 0, 1);
        
        var clears = stackalloc VkClearValue[]{new(0f,.0f,.0f,.0f),new(1f,0)};
        var passInfo = new VkRenderPassBeginInfo()
        {
            renderPass = renderpass,
            clearValueCount = 2,
            framebuffer = fb,
            pClearValues = clears,
            renderArea = new(target.width,target.height)
        };
        vkCmdBeginRenderPass(cb,&passInfo,VkSubpassContents.Inline);

        vkCmdBindPipeline(cb,VkPipelineBindPoint.Graphics,pipeline);
        vkCmdSetViewport(cb,0,new VkViewport(window.size.width,window.size.height));
        vkCmdSetScissor(cb,new(window.size));
        vkCmdBindVertexBuffer(cb,0,buff,0);
        vkCmdBindVertexBuffer(cb,1,buff,0);

        vkCmdDraw(cb,3,1,0,0);
        vkCmdEndRenderPass(cb);

        vkEndCommandBuffer(cb);
        var ss = renderSemaphores[frameNo % INFLIGHT_FRAME];
        vkQueueSubmit(API.graphicsQueue,
            new VkSubmitInfo()
                {commandBufferCount = 1, pCommandBuffers = &cb, signalSemaphoreCount = 1, pSignalSemaphores = &ss},
            default);
        
        
        present(window, ss,default);
    }

    private static unsafe void present(EngineWindow window, VkSemaphore waitSemaphore,VkFence signalfence)
    {
        int retryCount = 0;
        window.presenterState = (window.presenterState + 1) % window.SwapChainImages.Length;
        window.CleanupQueue[window.presenterState]();
        window.CleanupQueue[window.presenterState] = () => {};
        acquire: var rez = vkAcquireNextImageKHR(API.device,window.swapChain,UInt64.MaxValue,window.AcqforblitSemaphores[window.presenterState],default, out var index);
        switch (rez)
        {
            case VkResult.Success:
                break;
            case VkResult.SuboptimalKHR:
                fixed(VkSemaphore* aa=window.AcqforblitSemaphores)
                {
                    var semwaitinfo = new VkSemaphoreWaitInfo()
                    {
                        semaphoreCount = 1,
                        pSemaphores = &aa[window.presenterState],
                    };
                    vkWaitSemaphores(API.device,&semwaitinfo,ulong.MaxValue);
                }
                var relinfo = new VkReleaseSwapchainImagesInfoEXT()
                {
                    swapchain = window.swapChain,
                    imageIndexCount = 1,
                    pImageIndices = &index,
                };
                vkReleaseSwapchainImagesEXT(API.device, &relinfo);
                goto case VkResult.ErrorOutOfDateKHR;
            case VkResult.ErrorOutOfDateKHR:
                retryCount++;
                if (retryCount>10)
                    throw new Exception();
                API. CreateSwapchain(window,window.swapChain);
                goto acquire;
                
            default: throw new Exception();
        }

        VkCommandBuffer blit_cb = default;
        vkAllocateCommandBuffer(API.device, tpm, VkCommandBufferLevel.Primary, out blit_cb);//tmp

        vkBeginCommandBuffer(blit_cb, VkCommandBufferUsageFlags.OneTimeSubmit);
        
        var windowSwapChainImage = window.SwapChainImages[index];
        var bb = new VkImageCopy();
        
        bb.srcOffset=new(0,0,0);
        bb.dstOffset=new(0,0,0);
  
        bb.extent=new((int) window.size.width,(int) window.size.height,1);
        bb.srcSubresource = new(VkImageAspectFlags.Color, 0, 0, 1);
        bb.dstSubresource = new(VkImageAspectFlags.Color, 0, 0, 1);
        API.TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.General, 0, 1);
        VkClearColorValue a = new(1f, 1f, 1f, 1f);
        VkImageSubresourceRange b = new()
        {
            aspectMask = VkImageAspectFlags.Color,
            layerCount = 1,
            levelCount = 1,
            baseArrayLayer = 0,
            baseMipLevel = 0,
        };
        vkCmdClearColorImage(blit_cb,windowSwapChainImage.deviceImage,VkImageLayout.General,&a,1,&b);
        API.TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.TransferDstOptimal, 0, 1);
        var blit = new VkImageBlit()
        {
            srcSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
            dstSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
        };
        blit.dstOffsets[0] = new(0, 0, 0);
        blit.dstOffsets[1] = new((int) target.width, (int) target.height, 1);
        blit.srcOffsets[0] = new(0, 0, 0);
        blit.srcOffsets[1] = new((int) target.width, (int) target.height, 1);
        vkCmdBlitImage(blit_cb,target.deviceImage,target.layout[0],windowSwapChainImage.deviceImage,windowSwapChainImage.layout[0],1,&blit,VkFilter.Nearest);
        // vkCmdCopyImage(blit_cb,target.deviceImage,target.layout[0],windowSwapChainImage.deviceImage,windowSwapChainImage.layout[0],1,&bb);
        API.TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.PresentSrcKHR, 0, 1);

        vkEndCommandBuffer(blit_cb);

        var vkPipelineStageFlags = VkPipelineStageFlags.AllCommands;
        var waits = stackalloc VkSemaphore[] {waitSemaphore,window.AcqforblitSemaphores[window.presenterState] };
        var signal = window.ReadyToPresentToSwapchainSemaphores[index];
        var blitSub = new VkSubmitInfo()
        {
            commandBufferCount = 1,
            pCommandBuffers = &blit_cb,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &signal,
            waitSemaphoreCount = 2,
            pWaitSemaphores = waits,
            pWaitDstStageMask = &vkPipelineStageFlags
        };
        vkQueueSubmit(API.graphicsQueue, blitSub, default);

        vkQueuePresentKHR(API.graphicsQueue, signal, window.swapChain, index);
        frameNo++;
    }
}