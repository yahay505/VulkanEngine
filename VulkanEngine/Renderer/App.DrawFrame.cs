using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
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
        {//start
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

            vk.ResetFences(device, 1, fence); //only reset if we are rendering
            vk.ResetCommandPool(device, GetCurrentFrame().commandPool, CommandPoolResetFlags.ReleaseResourcesBit);
        }
        DoWhatMustBeDone();
        
      
        EnsureMeshRelatedBuffersAreSized();
        // If needed resize the object buffers
        var neededBufferSize = RenderManager.RenderObjects.Count;
        var currentBufferSize = GetCurrentFrame().hostRenderObjectsBufferSize;
        if (neededBufferSize>currentBufferSize)//resize buffers
        {
            ResizeRenderObjectRelatedBuffer(neededBufferSize, currentBufferSize);
        }
        
        RenderManager.WriteOutObjectData(GetCurrentFrame().hostRenderObjectsBufferAsSpan);
 
        var computeCommandBuffer = GetCurrentFrame().ComputeCommandBuffer;
        
        //vk wait last fr ame compute
        var commandBufferBeginInfo = new CommandBufferBeginInfo()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        vk.BeginCommandBuffer(computeCommandBuffer, &commandBufferBeginInfo)
            .Expect("failed to begin recording command buffer!");
        vk.CmdBindPipeline(computeCommandBuffer, PipelineBindPoint.Compute, ComputePipeline);
        fixed (DescriptorSet* pDescriptorSet = &ComputeDescriptorSet)
            vk.CmdBindDescriptorSets(computeCommandBuffer, PipelineBindPoint.Compute, ComputePipelineLayout, 0, 1, pDescriptorSet, 0, null);
        int ComputeWorkGroupSize=256;
        var dispatchSize = (uint) (RenderManager.RenderObjects.Count / ComputeWorkGroupSize)+1;
        vk.CmdDispatch(computeCommandBuffer, dispatchSize, 1, 1);
        vk.EndCommandBuffer(computeCommandBuffer)
            .Expect("failed to record command buffer!");
        var pWaitDstStageMask = stackalloc PipelineStageFlags[]{ PipelineStageFlags.ComputeShaderBit};
        var lastFrameComputeSemaphore = stackalloc Semaphore[]{GetLastFrame().ComputeSemaphore};
        var currentFrameComputeSemaphore = stackalloc Semaphore[]{GetCurrentFrame().ComputeSemaphore};
        var computeSubmitInfo = new SubmitInfo()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &computeCommandBuffer,
            PSignalSemaphores = currentFrameComputeSemaphore,
            SignalSemaphoreCount = 1,
            PWaitSemaphores = lastFrameComputeSemaphore,
            WaitSemaphoreCount = (uint) (CurrentFrame==0?0:1),
            PWaitDstStageMask = pWaitDstStageMask,
        };
        vk.QueueSubmit(computeQueue, 1, &computeSubmitInfo, default)
            .Expect("failed to submit compute command buffer!");
        
        var gfxCommandBuffer = GetCurrentFrame().GfxCommandBuffer;
 

        
        
        
        
        

        
        
        
        
        
        RecordCommandBuffer(gfxCommandBuffer, imageIndex);
        
        imGuiController.Render(gfxCommandBuffer,swapChainFramebuffers![imageIndex],swapChainExtent);
        
        vk.EndCommandBuffer(gfxCommandBuffer)
            .Expect("failed to record command buffer!");
        UpdateUniformBuffer(CurrentFrameIndex);
        
        var waitSemaphores = stackalloc Semaphore[] { GetCurrentFrame().RenderSemaphore };
        var waitStages= stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };
    
        var signalSemaphores = stackalloc[] { GetCurrentFrame().presentSemaphore };
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
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

    private static unsafe void UpdateUniformBuffer(int currentImage)
    {
        var time = (float)window!.Time;

        var translate = Matrix4X4<float>.Identity;
        var scale = Matrix4X4<float>.Identity;
        var rot = Matrix4X4.CreateFromAxisAngle<float>(new Vector3D<float>(0, 0, 1), time * Scalar.DegreesToRadians(90.0f));

        Camera camera = new Camera();
        var cameraPosition = new float3(2, 2, 3);
        var objectPosition = new float3(0, 0, 0);
        var cameraUpVector = new float3(0, 0, 1);
        var fov = 45.0f;

        var nearPlaneDistance = 0.1f;
        var farPlaneDistance = 10.0f;
        var ubo = new UniformBufferObject
        {
            model = translate * rot * scale,
            view = Matrix4X4.CreateLookAt(cameraPosition, objectPosition, cameraUpVector),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(fov),
                (float) swapChainExtent.Width / swapChainExtent.Height, nearPlaneDistance, farPlaneDistance),
        };
        ubo.proj.M22 *= -1;
        
        
        var size = (nuint) sizeof(UniformBufferObject);
        var data = uniformBuffersMapped[currentImage]!;
        Unsafe.CopyBlock(data, &ubo, (uint)size);
    }
}