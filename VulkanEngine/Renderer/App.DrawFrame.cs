using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using VulkanEngine.Phases.FrameRender;
using Vortice.Vulkan;
using VulkanEngine.Renderer.Text;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    // static Semaphore[] imageAvailableSemaphores;
    // static Semaphore[] renderFinishedSemaphores;
    // static Fence[] inFlightFences;
    private static long last_tick;
    public static float deltaTime;
    // cleanup dangerous semaphore with signal pending from vkAcquireNextImageKHR (tie it to a specific queue)
// https://github.com/KhronosGroup/Vulkan-Docs/issues/1059
    static unsafe void cleanupUnsafeSemaphore( VkQueue queue, VkSemaphore semaphore ){
        const VkPipelineStageFlags psw = VK_PIPELINE_STAGE_BOTTOM_OF_PIPE_BIT;
        VkSubmitInfo submit_info = new(){};
        submit_info.waitSemaphoreCount = 1;
        submit_info.pWaitSemaphores = &semaphore;

        vkQueueSubmit( queue, 1, &submit_info, default );
    }
    private static unsafe void DrawFrame(EngineWindow window, out bool retry)
    {
        retry = false;
        
        
        uint imageIndex = 999;
        {
            // vkDeviceWaitIdle(device);
            //start
            var fence = GetCurrentFrame().renderFence;
            start: vkWaitForFences(device, 1, &fence, true, ulong.MaxValue)
                .Expect("failed to wait for fence!");
            
            var result = vkAcquireNextImageKHR(device, window.swapChain, ulong.MaxValue,
                GetCurrentFrame().RenderSemaphore, default, &imageIndex);

            switch (result)
            {
                case VkResult.Success:
                    break;
                case VkResult.SuboptimalKHR:
                case VkResult.ErrorOutOfDateKHR:

                  
                    ResizeSwapChain(window,ChooseSwapExtent(window).ToInt2());
                    
                     var renderSemaphore = GetCurrentFrame().RenderSemaphore;
                     var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
                     var psub = new VkSubmitInfo()
                    {
                         commandBufferCount = 0,
                         pCommandBuffers = default,
                         waitSemaphoreCount = 1,
                         pWaitSemaphores = &renderSemaphore,
                         signalSemaphoreCount = 0,
                         pSignalSemaphores = default,
                         pWaitDstStageMask = &pipelineStageFlags,
                         pNext = default
                     };
                    // vkDeviceWaitIdle(device);
                     vkResetFences(device, fence).Expect(); //reset since were gonna re-set it immediately
                     vkQueueSubmit(graphicsQueue,psub,fence).Expect();
                     
                    
                     vkDestroySemaphore(device,GetCurrentFrame().RenderSemaphore,null);

                     vkCreateSemaphore(device,out GetCurrentFrame().RenderSemaphore);
                    retry = true;
                    if (result==VkResult.ErrorOutOfDateKHR)
                    {
                        goto start;
                    }
                    break;
                default:
                    result.Expect("failed to acquire swap chain image!");
                    throw new UnreachableException();
            }

            // if (fence.Handle != default)
            // {
            //     vkWaitForFences(device, 1, &fence, true, ulong.MaxValue)
            //         .Expect("failed to wait for fence!");
            // }

            vkResetFences(device, 1, &fence).Expect(); //only reset if we are rendering
            vkResetCommandPool(device, GetCurrentFrame().commandPool, VkCommandPoolResetFlags.ReleaseResources)
                .Expect();
        }

        ExecuteCleanupScheduledForCurrentFrame();
        
        EnsureMeshRelatedBuffersAreSized();
        

        // EnsureRenderObjectRelatedBuffersAreSized();

        
        // GPURenderRegistry.WriteOutObjectData(GetCurrentFrame().hostRenderObjectsBufferAsSpan);
        var objectCount=Volatile.Read(ref WriteoutRenderObjects.writenObjectCount);
        GetCurrentFrame().computeInputConfig->objectCount = (uint) objectCount;
        
        
        var computeCommandBuffer = GetCurrentFrame().ComputeCommandBuffer;
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        DrawFrame_ComputePart(computeCommandBuffer, objectCount);
        
        
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        
        var gfxCommandBuffer = GetCurrentFrame().GfxCommandBuffer;
        
        DrawFrame_GFXPart(window,gfxCommandBuffer, (int)imageIndex, objectCount);
        
        
        vkEndCommandBuffer(gfxCommandBuffer)
            .Expect("failed to record command buffer!");
        UpdateUniformBuffer(CurrentFrameIndex);
        
        var waitSemaphores = stackalloc VkSemaphore[] { GetCurrentFrame().RenderSemaphore, default};
        var waitStages= stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.ColorAttachmentOutput,VkPipelineStageFlags.DrawIndirect |VkPipelineStageFlags.VertexShader };
        if (DrawIndirectCountAvaliable)
        {
            waitSemaphores[1] = GetCurrentFrame().ComputeSemaphore2;
        }
        var signalSemaphores = stackalloc[] { GetCurrentFrame().presentSemaphore };
        var submitInfo = new VkSubmitInfo
       {
            waitSemaphoreCount = (uint) (DrawIndirectCountAvaliable?2:1),
            pWaitSemaphores = waitSemaphores,
            pWaitDstStageMask = waitStages,
            commandBufferCount = 1,
            pCommandBuffers = &gfxCommandBuffer,
            signalSemaphoreCount = 1,
            pSignalSemaphores = signalSemaphores
        };
        
        vkQueueSubmit(graphicsQueue,1, &submitInfo, GetCurrentFrame().renderFence)
            .Expect("failed to submit draw command buffer!");

        var swapChains = stackalloc VkSwapchainKHR[] { window.swapChain };

        var presentinfo = new VkPresentInfoKHR
       {
            waitSemaphoreCount = 1,
            pWaitSemaphores = signalSemaphores,
            swapchainCount = 1,
            pSwapchains = swapChains,
            pImageIndices = &imageIndex,
            pResults = null,
        };
        var present = vkQueuePresentKHR(presentQueue, &presentinfo);
        // return;
        if (present is VkResult.ErrorOutOfDateKHR /*or Result.SuboptimalKhr*/ || FramebufferResized)
        {
            ResizeSwapChain(window,ChooseSwapExtent(window).ToInt2());
            retry = true;
            return;
        }
        else if(present!=VkResult.SuboptimalKHR)
        {
            present.Expect("failed to present swap chain image!");
        }
        
    }

    private static unsafe void DrawFrame_ComputePart(VkCommandBuffer computeCommandBuffer, int objectCount)
    {
        //vk wait last fr ame compute
        var commandBufferBeginInfo = new VkCommandBufferBeginInfo()
       {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        vkBeginCommandBuffer(computeCommandBuffer, &commandBufferBeginInfo)
            .Expect("failed to begin recording command buffer!");
        //todo move
        MaterialManager.Sync(computeCommandBuffer,VkPipelineStageFlags.ComputeShader|VkPipelineStageFlags.AllGraphics,VkAccessFlags.ShaderRead);
        
        
        var vkBufferCopy = new VkBufferCopy(){srcOffset = 0, dstOffset = 0, size = (uint) GetCurrentFrame().hostRenderObjectsBufferSizeInBytes};
        vkCmdCopyBuffer(computeCommandBuffer, GetCurrentFrame().hostRenderObjectsBuffer,
            GlobalData.deviceRenderObjectsBuffer, 1,
            &vkBufferCopy);
        vkCmdBindPipeline(computeCommandBuffer, VkPipelineBindPoint.Compute, ComputePipeline);

        var pDescriptorSet = stackalloc VkDescriptorSet[] {GetCurrentFrame().descriptorSets.Compute};
        vkCmdBindDescriptorSets(computeCommandBuffer, VkPipelineBindPoint.Compute, ComputePipelineLayout, 0, 1,
            pDescriptorSet, 0, null);
        //MaterialManager.Bind(computeCommandBuffer,0);
        
        int ComputeWorkGroupSize = 16;
        var dispatchSize = (uint) (objectCount / ComputeWorkGroupSize) + 1;
        VkBufferMemoryBarrier to0first4bytes = new()
       {
            buffer = GlobalData.deviceIndirectDrawBuffer,
            srcAccessMask = VkAccessFlags.ShaderRead,
            dstAccessMask = VkAccessFlags.TransferWrite,
            offset = 0,
            size = (nuint) 64,
        };
        vkCmdPipelineBarrier(computeCommandBuffer,
            VkPipelineStageFlags.ComputeShader,
            VkPipelineStageFlags.Transfer,
            VkDependencyFlags.None,
            0,
            null,
            1,
            &to0first4bytes,
            0,
            null);

        //zero out atomic counter from last frame
        vkCmdFillBuffer(computeCommandBuffer, GlobalData.deviceIndirectDrawBuffer, 0, 64, 0);
        var transfertocomputebarrier = stackalloc VkBufferMemoryBarrier[]
        {
            new()
           {
                buffer = GlobalData.deviceRenderObjectsBuffer,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.ShaderRead,
                offset = 0,
                size = (nuint) GlobalData.deviceRenderObjectsBufferSizeInBytes,
            },
            new()
           {
                buffer = GlobalData.deviceIndirectDrawBuffer,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.ShaderWrite | VkAccessFlags.ShaderRead,
                offset = 0,
               size = (nuint) GlobalData.deviceIndirectDrawBufferSizeInBytes,
            }
        };
        vkCmdPipelineBarrier(computeCommandBuffer,
            VkPipelineStageFlags.Transfer | VkPipelineStageFlags.VertexShader,
            VkPipelineStageFlags.ComputeShader,
            VkDependencyFlags.None,
            0,
            null,
            2,
            transfertocomputebarrier,
            0,
            null);

        vkCmdDispatch(computeCommandBuffer, dispatchSize, 1, 1);
        var computeToRenderBarrier = stackalloc VkBufferMemoryBarrier[]
        {
            new()
           {
                buffer = GlobalData.deviceIndirectDrawBuffer,
                srcAccessMask = VkAccessFlags.ShaderWrite,
                dstAccessMask = VkAccessFlags.ShaderRead,
                offset = 0,
               size = (nuint) GlobalData.deviceIndirectDrawBufferSizeInBytes,
            },
            new()
           {
                buffer = GlobalData.deviceRenderObjectsBuffer,
                srcAccessMask = VkAccessFlags.ShaderRead,
                dstAccessMask = VkAccessFlags.TransferWrite,
                offset = 0,
               size = (nuint) GlobalData.deviceRenderObjectsBufferSizeInBytes,
            }
        };
        vkCmdPipelineBarrier(computeCommandBuffer,
            VkPipelineStageFlags.ComputeShader,
            VkPipelineStageFlags.ComputeShader | VkPipelineStageFlags.Transfer,
            VkDependencyFlags.None,
            0,
            null,
            2,
            computeToRenderBarrier,
            0,
            null);
        if (!DrawIndirectCountAvaliable)
        {
            var ComputeOutputBarrier = new VkBufferMemoryBarrier()
           {
                buffer = GlobalData.deviceIndirectDrawBuffer,
                srcAccessMask = VkAccessFlags.MemoryWrite,
                dstAccessMask = VkAccessFlags.TransferRead,
                offset = 0,
               size = 4,
            };
            vkCmdPipelineBarrier(computeCommandBuffer,
                VkPipelineStageFlags.ComputeShader,
                VkPipelineStageFlags.Transfer,
                VkDependencyFlags.None,
                0,
                null,
                1,
                &ComputeOutputBarrier,
                0,
                null);
            var first4bytes = new VkBufferCopy(){srcOffset=0, dstOffset=0, size = 4};
            //copy max draw count to device
            vkCmdCopyBuffer(computeCommandBuffer, GlobalData.deviceIndirectDrawBuffer, GlobalData.ReadBackBuffer, 1,
                &first4bytes);


            var transferToReadBackBarrier = new VkBufferMemoryBarrier()
           {
                buffer = GlobalData.ReadBackBuffer,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.HostRead,
                offset = 0,
               size = 4,
            };
            vkCmdPipelineBarrier(computeCommandBuffer,
                VkPipelineStageFlags.Transfer,
                VkPipelineStageFlags.Host,
                VkDependencyFlags.None,
                0,
                null,
                1,
                &transferToReadBackBarrier,
                0,
                null);
        }

        vkEndCommandBuffer(computeCommandBuffer)
            .Expect("failed to record command buffer!");
        var _pWaitdstStageMask = stackalloc VkPipelineStageFlags[] {VkPipelineStageFlags.ComputeShader};
        var pWaitSemaphores = stackalloc VkSemaphore[] {GetLastFrame().ComputeSemaphore};
        var pSignalSemaphores = stackalloc VkSemaphore[] {GetCurrentFrame().ComputeSemaphore, default};
        if (DrawIndirectCountAvaliable)
        {
            pSignalSemaphores[1] = GetCurrentFrame().ComputeSemaphore2;
        }

        var computeSubmitInfo = new VkSubmitInfo()
       {
            commandBufferCount = 1,
            pCommandBuffers = &computeCommandBuffer,
            signalSemaphoreCount = (uint) (DrawIndirectCountAvaliable ? 2 : 1),
            pSignalSemaphores = pSignalSemaphores,
            waitSemaphoreCount = (uint) (CurrentFrame == 0 ? 0 : 1),
            pWaitSemaphores = pWaitSemaphores,
            pWaitDstStageMask = _pWaitdstStageMask,
        };

        //Console.WriteLine($"submitinfo: \n commandBufferCount: {computeSubmitInfo.commandBufferCount}\n signalSemaphoreCount: {computeSubmitInfo.signalSemaphoreCount}\n waitSemaphoreCount: {computeSubmitInfo.waitSemaphoreCount}\n\n pCommandBuffers: {(nuint)computeSubmitInfo.pCommandBuffers:X8}\n\n pSignalSemaphores: {(nuint)computeSubmitInfo.pSignalSemaphores:X8}\n\n pWaitSemaphores: {(nuint)computeSubmitInfo.pWaitSemaphores:X8}");
        vkQueueSubmit(computeQueue, 1, &computeSubmitInfo,
                DrawIndirectCountAvaliable ? default : GetCurrentFrame().computeFence)
            .Expect("failed to submit compute command buffer!");
    }

    public static void UpdateTime()
    {
        var t = Stopwatch.GetTimestamp();
        var deltaSeconds = (t - last_tick) / (double) Stopwatch.Frequency;
        last_tick = t;
        deltaTime= (float) deltaSeconds;
    }

    private static unsafe void UpdateUniformBuffer(int index)
    {
        
        
        var time = (float)Stopwatch.GetTimestamp() / (float)Stopwatch.Frequency;

        var translate = Matrix4X4<float>.Identity;
        var scale = Matrix4X4<float>.Identity;
        var rot = Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f));

        // Camera camera = new Camera();
        // var cameraPosition = new float3(2, 2, 3);
        // var objectPosition = new float3(0, 0, 0);
        // var cameraUpVector = new float3(0, 0, 1);
        // var fov = 45.0f;
        //
        // var nearPlaneDistance = 0.1f;
        // var farPlaneDistance = 10.0f;
        
        
        
        var ubo = new UniformBufferObject
        {
            viewproj = currentCamera.view*currentCamera.proj,
        };
        
        
        var size = (nuint) sizeof(UniformBufferObject);
        var data = FrameData[index].uniformBufferMapped!;
        Unsafe.CopyBlock(data, &ubo, (uint)size);
    }

    public static VkFramebuffer CreateFrameBuffer(EngineWindow window, int frameno)
    {
        unsafe
        {
            VkFramebuffer fb;
            var attachments = stackalloc[] { window.SwapChainImages[frameno].ImageView, window.depthImage.ImageView };
            var framebufferInfo = new VkFramebufferCreateInfo()
           {
                renderPass = RenderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                flags = VkFramebufferCreateFlags.None,
                width = window.size.width,
                height = window.size.height,
                layers = 1,
            };
            vkCreateFramebuffer(device, &framebufferInfo, null, out fb);
            FrameCleanup[CurrentFrameIndex]+=()=>vkDestroyFramebuffer(device,fb,null);
            return fb;
        }
}
    private static unsafe void DrawFrame_GFXPart(EngineWindow window,VkCommandBuffer commandBuffer, int imageIndex, int maxDrawCount)
    {
        var beginInfo = new VkCommandBufferBeginInfo
       {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit,
            pInheritanceInfo = null,
        };
        vkBeginCommandBuffer(commandBuffer, &beginInfo)
            .Expect("failed to begin recording command buffer!");

        var clearValues = stackalloc VkClearValue[]
        {
            new(0f,0f,0.0f),
            new(1,0)
        };

        var frameBuffer = CreateFrameBuffer(window, imageIndex);
        var renderPassInfo = new VkRenderPassBeginInfo
       {
            renderPass = RenderPass,
            framebuffer = frameBuffer,
            renderArea = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = window.size,
            },
            clearValueCount = 2,
            pClearValues = (clearValues)
        };
        
        var viewPort = new VkViewport()
        {
            x = 0,
            y = 0,
            width = window.size.width,
            height = window.size.height,
            minDepth = 0,
            maxDepth = 1,
        };
        var scissor = new VkRect2D
        {
            offset = new VkOffset2D(0, 0),
            extent = window.size,
        };
        // var memoryBarrier = new MemoryBarrier
        //{
        //     srcAccessMask = VkAccessFlags.ShaderWrite,
        //     dstAccessMask = VkAccessFlags.TransferWrite,
        // };
        // vkCmdPipelineBarrier(commandBuffer,
        //     VkPipelineStageFlags.FragmentShader,
        //     VkPipelineStageFlags.Transfer,
        //     0,
        //     1,
        //     memoryBarrier,
        //     0,
        //     null,
        //     0,
        //     null);
        vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, VkSubpassContents.Inline);
        // vkCmdCopyBuffer();
        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, GraphicsPipeline);
        vkCmdSetViewport(commandBuffer,0,1,&viewPort);
        vkCmdSetScissor(commandBuffer,0,1,&scissor);
        var vertexBuffers =stackalloc []{(VkBuffer)GlobalData.vertexBuffer};
        var offsets = stackalloc [] {0ul};
        
        // vk!.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        vkCmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        var pDescriptorSet = stackalloc VkDescriptorSet[] {GetCurrentFrame().descriptorSets.GFX};
        vkCmdBindIndexBuffer(commandBuffer, GlobalData.indexBuffer, 0, VkIndexType.Uint32);
        vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, GfxPipelineLayout, 0, 1, pDescriptorSet, 0, null);
        // vkCmdBindDescriptorSets(commandBuffer,VkPipelineBindPoint.Graphics, TextureManager.descSetLayout,1,1,default,0,null);

        if (DrawIndirectCountAvaliable)
        {
            vkCmdDrawIndexedIndirectCount(commandBuffer,
                GlobalData.deviceIndirectDrawBuffer,
                ComputeOutSSBOStartOffset,
                GlobalData.deviceIndirectDrawBuffer,
                0,
                (uint) maxDrawCount,
                (uint) sizeof(GPUStructs.ComputeDrawOutput));
        }
        else
        {
            vkWaitForFences(device,1,&GetCurrentFrame().ptr()->computeFence,true,ulong.MaxValue);
            var postCullCount = *(int*) GlobalData.ReadBackBufferPtr;
            vkResetFences(device,1,&GetCurrentFrame().ptr()->computeFence);
            vkCmdDrawIndexedIndirect(commandBuffer,
                GlobalData.deviceIndirectDrawBuffer,
                ComputeOutSSBOStartOffset,
                (uint) postCullCount,
                (uint) sizeof(GPUStructs.ComputeDrawOutput));
        }
        DebugTextRenderer.Draw(commandBuffer);
        
        // vk!.CmdDraw(commandBuffer, (uint) vertices.Length, 1, 0, 0);
        
        
        //imGuiController.Render(commandBuffer,frameBuffer,window.size);

        vkCmdEndRenderPass(commandBuffer);
    }
}
