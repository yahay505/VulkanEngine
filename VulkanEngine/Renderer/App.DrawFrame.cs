using System.Diagnostics;
using System.Runtime.CompilerServices;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using VulkanEngine.Phases.FrameRender;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    // static Semaphore[] imageAvailableSemaphores;
    // static Semaphore[] renderFinishedSemaphores;
    // static Fence[] inFlightFences;
    private static long last_tick;
    public static float deltaTime;
    private static unsafe void DrawFrame()
    {
        uint imageIndex = 999;
        {
            //start
            var fence = GetCurrentFrame().renderFence;
            vk.WaitForFences(device, 1, fence, true, ulong.MaxValue)
                .Expect("failed to wait for fence!");
            var result = khrSwapChain.AcquireNextImage(device, swapChain, ulong.MaxValue,
                GetCurrentFrame().RenderSemaphore, default, &imageIndex);

            switch (result)
            {
                case Result.Success:
                case Result.SuboptimalKhr:
                    break;
                case Result.ErrorOutOfDateKhr:
                    RecreateSwapChain();

                    return;
                default:
                    throw new Exception("failed to acquire swap chain image!");
            }

            if (fence.Handle != default)
            {
                vk.WaitForFences(device, 1, fence, true, ulong.MaxValue)
                    .Expect("failed to wait for fence!");
            }

            vk.ResetFences(device, 1, fence).Expect(); //only reset if we are rendering
            vk.ResetCommandPool(device, GetCurrentFrame().commandPool, CommandPoolResetFlags.ReleaseResourcesBit)
                .Expect();
        }

        ExecuteCleanupScheduledForCurrentFrame();
        
        EnsureMeshRelatedBuffersAreSized();

        // EnsureRenderObjectRelatedBuffersAreSized();

        
        // GPURenderRegistry.WriteOutObjectData(GetCurrentFrame().hostRenderObjectsBufferAsSpan);
        var objectCount=Volatile.Read(ref WriteoutRenderObjects.writenObjectCount);
        GetCurrentFrame().computeInputConfig->objectCount = (uint) objectCount;
        
        
        var computeCommandBuffer = GetCurrentFrame().ComputeCommandBuffer;
        
        //vk wait last fr ame compute
        var commandBufferBeginInfo = new CommandBufferBeginInfo()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        vk.BeginCommandBuffer(computeCommandBuffer, &commandBufferBeginInfo)
            .Expect("failed to begin recording command buffer!");
        
        vk.CmdCopyBuffer(computeCommandBuffer, GetCurrentFrame().hostRenderObjectsBuffer, GlobalData.deviceRenderObjectsBuffer, 1, new BufferCopy(0, 0, (uint)GetCurrentFrame().hostRenderObjectsBufferSizeInBytes));
        vk.CmdBindPipeline(computeCommandBuffer, PipelineBindPoint.Compute, ComputePipeline);
        
        var pDescriptorSet = stackalloc DescriptorSet[] { GetCurrentFrame().descriptorSets.Compute };
        vk.CmdBindDescriptorSets(computeCommandBuffer, PipelineBindPoint.Compute, ComputePipelineLayout, 0, 1, pDescriptorSet, 0, null);
        int ComputeWorkGroupSize=16;
        var dispatchSize = (uint) (objectCount / ComputeWorkGroupSize)+1;
        BufferMemoryBarrier to0first4bytes = new()
        {
            SType = StructureType.BufferMemoryBarrier,
            Buffer = GlobalData.deviceIndirectDrawBuffer,
            SrcAccessMask = AccessFlags.ShaderReadBit,
            DstAccessMask = AccessFlags.TransferWriteBit,
            Offset = 0,
            Size = (nuint)64,
        };
        vk.CmdPipelineBarrier(computeCommandBuffer,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.TransferBit,
            DependencyFlags.None,
            0,
            null,
            1,
            &to0first4bytes,
            0,
            null);
        
        //zero out atomic counter from last frame
        vk.CmdFillBuffer(computeCommandBuffer, GlobalData.deviceIndirectDrawBuffer, 0, 64, 0);
        var transfertocomputebarrier = stackalloc BufferMemoryBarrier[]{
                new()
                {
                    SType = StructureType.BufferMemoryBarrier,
                    Buffer = GlobalData.deviceRenderObjectsBuffer,
                    SrcAccessMask = AccessFlags.TransferWriteBit,
                    DstAccessMask = AccessFlags.ShaderReadBit,
                    Offset = 0,
                    Size = (nuint) GlobalData.deviceRenderObjectsBufferSizeInBytes,
                },
                new ()
                {
                    SType = StructureType.BufferMemoryBarrier,
                    Buffer = GlobalData.deviceIndirectDrawBuffer,
                    SrcAccessMask = AccessFlags.TransferWriteBit,
                    DstAccessMask = AccessFlags.ShaderWriteBit|AccessFlags.ShaderReadBit,
                    Offset = 0,
                    Size = (nuint) GlobalData.deviceIndirectDrawBufferSizeInBytes,
                }
            };
        vk.CmdPipelineBarrier(computeCommandBuffer,
            PipelineStageFlags.TransferBit|PipelineStageFlags.VertexShaderBit,
            PipelineStageFlags.ComputeShaderBit,
            DependencyFlags.None,
            0,
            null,
            2,
            transfertocomputebarrier,
            0,
            null);
        
        vk.CmdDispatch(computeCommandBuffer, dispatchSize, 1, 1);
        var computeToRenderBarrier = stackalloc BufferMemoryBarrier[]{
            new()
            {
                SType = StructureType.BufferMemoryBarrier,
                Buffer = GlobalData.deviceIndirectDrawBuffer,
                SrcAccessMask = AccessFlags.ShaderWriteBit,
                DstAccessMask = AccessFlags.ShaderReadBit,
                Offset = 0,
                Size = (nuint) GlobalData.deviceIndirectDrawBufferSizeInBytes,
            },
            new()
            {
                SType = StructureType.BufferMemoryBarrier,
                Buffer = GlobalData.deviceRenderObjectsBuffer,
                SrcAccessMask = AccessFlags.ShaderReadBit,
                DstAccessMask = AccessFlags.TransferWriteBit,
                Offset = 0,
                Size = (nuint) GlobalData.deviceRenderObjectsBufferSizeInBytes,
            }
            
        };
        vk.CmdPipelineBarrier(computeCommandBuffer,
            PipelineStageFlags.ComputeShaderBit,
            PipelineStageFlags.ComputeShaderBit|PipelineStageFlags.TransferBit,
            DependencyFlags.None,
            0,
            null,
            2,
            computeToRenderBarrier,
            0,
            null);
        if (!DrawIndirectCountAvaliable)
        {
            var ComputeOutputBarrier = new BufferMemoryBarrier()
            {
                SType = StructureType.BufferMemoryBarrier,
                Buffer = GlobalData.deviceIndirectDrawBuffer,
                SrcAccessMask = AccessFlags.MemoryWriteBit,
                DstAccessMask = AccessFlags.TransferReadBit,
                Offset = 0,
                Size = 4,
            };
            vk.CmdPipelineBarrier(computeCommandBuffer,
                PipelineStageFlags.ComputeShaderBit,
                PipelineStageFlags.TransferBit,
                DependencyFlags.None,
                0,
                null,
                1,
                &ComputeOutputBarrier,
                0,
                null);
            var first4bytes = new BufferCopy(0, 0, 4);
            //copy max draw count to device
            vk.CmdCopyBuffer(computeCommandBuffer, GlobalData.deviceIndirectDrawBuffer, GlobalData.ReadBackBuffer,1,first4bytes);


            var transferToReadBackBarrier = new BufferMemoryBarrier()
            {
                SType = StructureType.BufferMemoryBarrier,
                Buffer = GlobalData.ReadBackBuffer,
                SrcAccessMask = AccessFlags.TransferWriteBit,
                DstAccessMask = AccessFlags.HostReadBit,
                Offset = 0,
                Size = 4,
            };
            vk.CmdPipelineBarrier(computeCommandBuffer,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.HostBit,
                DependencyFlags.None,
                0,
                null,
                1,
                &transferToReadBackBarrier,
                0,
                null);
        }
        
        vk.EndCommandBuffer(computeCommandBuffer)
            .Expect("failed to record command buffer!");
        var pWaitDstStageMask = stackalloc PipelineStageFlags[]{ PipelineStageFlags.ComputeShaderBit};
        var pWaitSemaphores = stackalloc Semaphore[]{GetLastFrame().ComputeSemaphore};
        var pSignalSemaphores = stackalloc Semaphore[]{GetCurrentFrame().ComputeSemaphore,default};
        if (DrawIndirectCountAvaliable)
        {
            pSignalSemaphores[1] = GetCurrentFrame().ComputeSemaphore2;
        }
        var computeSubmitInfo = new SubmitInfo()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &computeCommandBuffer,
            SignalSemaphoreCount = (uint) (DrawIndirectCountAvaliable?2:1),
            PSignalSemaphores = pSignalSemaphores,
            WaitSemaphoreCount = (uint) (CurrentFrame==0?0:1),
            PWaitSemaphores = pWaitSemaphores,
            PWaitDstStageMask = pWaitDstStageMask,
        };
        
        
        vk.QueueSubmit(computeQueue, 1, &computeSubmitInfo, DrawIndirectCountAvaliable?default:GetCurrentFrame().computeFence)
            .Expect("failed to submit compute command buffer!");


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        
        
        var gfxCommandBuffer = GetCurrentFrame().GfxCommandBuffer;
        
        RecordCommandBuffer(gfxCommandBuffer, imageIndex, objectCount);
        
        
        vk.EndCommandBuffer(gfxCommandBuffer)
            .Expect("failed to record command buffer!");
        UpdateUniformBuffer(CurrentFrameIndex);
        
        var waitSemaphores = stackalloc Semaphore[] { GetCurrentFrame().RenderSemaphore, default};
        var waitStages= stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit,PipelineStageFlags.DrawIndirectBit|PipelineStageFlags.VertexShaderBit };
        if (DrawIndirectCountAvaliable)
        {
            waitSemaphores[1] = GetCurrentFrame().ComputeSemaphore2;
        }
        var signalSemaphores = stackalloc[] { GetCurrentFrame().presentSemaphore };
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = (uint) (DrawIndirectCountAvaliable?2:1),
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &gfxCommandBuffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
        };
        
        vk.QueueSubmit(graphicsQueue,1, &submitInfo, GetCurrentFrame().renderFence)
            .Expect("failed to submit draw command buffer!");

        var swapChains = stackalloc SwapchainKHR[] { swapChain };

        var presentinfo = new PresentInfoKHR
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = signalSemaphores,
            SwapchainCount = 1,
            PSwapchains = swapChains,
            PImageIndices = &imageIndex,
            PResults = null,
        };
        var present = khrSwapChain.QueuePresent(presentQueue, &presentinfo);
        // return;
        if (present is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr || FramebufferResized)
        {
            RecreateSwapChain();
        }
        else
        {
            present.Expect("failed to present swap chain image!");
        }
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
        
        
        var time = (float)window!.Time;

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


    private static unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex, int maxDrawCount)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit,
            PInheritanceInfo = null,
        };
        vk.BeginCommandBuffer(commandBuffer, beginInfo)
            .Expect("failed to begin recording command buffer!");

        var clearValues = stackalloc ClearValue[]
        {
            new(new(0f,0f,0f,0.0f)),
            new(null,new(1,0))
        };
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = RenderPass,
            Framebuffer = swapChainFramebuffers![imageIndex],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChainExtent,
            },
            ClearValueCount = 2,
            
            PClearValues = (clearValues)
        };
        
        var viewPort = new Viewport()
        {
            X = 0,
            Y = 0,
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };
        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = swapChainExtent,
        };
        // var memoryBarrier = new MemoryBarrier
        // {
        //     SType = StructureType.MemoryBarrier,
        //     SrcAccessMask = AccessFlags.ShaderWriteBit,
        //     DstAccessMask = AccessFlags.TransferWriteBit,
        // };
        // vk.CmdPipelineBarrier(commandBuffer,
        //     PipelineStageFlags.FragmentShaderBit,
        //     PipelineStageFlags.TransferBit,
        //     0,
        //     1,
        //     memoryBarrier,
        //     0,
        //     null,
        //     0,
        //     null);
        vk.CmdBeginRenderPass(commandBuffer, renderPassInfo, SubpassContents.Inline);
        // vk.CmdCopyBuffer();
        vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, GraphicsPipeline);
        vk.CmdSetViewport(commandBuffer,0,1,&viewPort);
        vk.CmdSetScissor(commandBuffer,0,1,&scissor);
        var vertexBuffers =stackalloc []{(Buffer)GlobalData.vertexBuffer};
        var offsets = stackalloc []{(ulong)0};
        
        // vk!.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        var pDescriptorSet = stackalloc DescriptorSet[] {GetCurrentFrame().descriptorSets.GFX};
        vk.CmdBindIndexBuffer(commandBuffer, GlobalData.indexBuffer, 0, IndexType.Uint32);
        vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, GfxPipelineLayout, 0, 1, pDescriptorSet, 0, null);


        if (DrawIndirectCountAvaliable)
        {
            vk.CmdDrawIndexedIndirectCount(commandBuffer,
                GlobalData.deviceIndirectDrawBuffer,
                ComputeOutSSBOStartOffset,
                GlobalData.deviceIndirectDrawBuffer,
                0,
                (uint) maxDrawCount,
                (uint) sizeof(GPUStructs.ComputeDrawOutput));
        }
        else
        {
            vk.WaitForFences(device,1,GetCurrentFrame().computeFence,true,ulong.MaxValue);
            var postCullCount = *(int*) GlobalData.ReadBackBufferPtr;
            vk.ResetFences(device,1,GetCurrentFrame().computeFence);
            vk.CmdDrawIndexedIndirect(commandBuffer,
                GlobalData.deviceIndirectDrawBuffer,
                ComputeOutSSBOStartOffset,
                (uint) postCullCount,
                (uint) sizeof(GPUStructs.ComputeDrawOutput));
        }

        
        // vk!.CmdDraw(commandBuffer, (uint) vertices.Length, 1, 0, 0);
        
        
        
        imGuiController.Render(commandBuffer,swapChainFramebuffers![imageIndex],swapChainExtent);

        vk.CmdEndRenderPass(commandBuffer);
    }
}
