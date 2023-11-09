using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    static Semaphore[] imageAvailableSemaphores;
    static Semaphore[] renderFinishedSemaphores;
    static Fence[] inFlightFences;
    private static long last_tick;
    private static unsafe void DrawFrame()
    {
        var t= Stopwatch.GetTimestamp();
        var deltaSeconds = (t - last_tick) / (double)Stopwatch.Frequency;
        last_tick = t;
        Console.WriteLine($"{deltaSeconds*1000,5:F2}ms");
        var fence= inFlightFences[CurrentFrameIndex];
        vk.WaitForFences(device, 1,  fence, true, ulong.MaxValue)
            .Expect("failed to wait for fence!");
        uint imageIndex = 999;
        var result = khrSwapChain.AcquireNextImage(device, swapChain, ulong.MaxValue, imageAvailableSemaphores[CurrentFrameIndex], default, &imageIndex);
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
        if (inFlightFences[CurrentFrameIndex].Handle != default)
        {
            vk.WaitForFences(device, 1, inFlightFences[CurrentFrameIndex], true, ulong.MaxValue)
                .Expect("failed to wait for fence!");
        }
        vk.ResetFences(device, 1, fence);//only reset if we are rendering
        vk.ResetCommandBuffer(CommandBuffers[CurrentFrameIndex], 0);
        RecordCommandBuffer(CommandBuffers[CurrentFrameIndex], imageIndex);
        UpdateUniformBuffer(CurrentFrameIndex);
        
        var waitSemaphores = stackalloc Semaphore[] { imageAvailableSemaphores[CurrentFrameIndex] };
        var waitStages= stackalloc PipelineStageFlags[] { PipelineStageFlags.ColorAttachmentOutputBit };
        var buffer = CommandBuffers[CurrentFrameIndex];
        var signalSemaphores = stackalloc[] { renderFinishedSemaphores[CurrentFrameIndex] };
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = waitSemaphores,
            PWaitDstStageMask = waitStages,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = signalSemaphores
        };
        
        vk.QueueSubmit(graphicsQueue,1, &submitInfo, inFlightFences[CurrentFrameIndex])
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

    private static void UpdateUniformBuffer(int currentImage)
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
            model = translate * rot  * scale,
            view = Matrix4X4.CreateLookAt(cameraPosition, objectPosition, cameraUpVector),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(fov), (float)swapChainExtent.Width / swapChainExtent.Height, nearPlaneDistance, farPlaneDistance),
        };
        ubo.proj.M22 *= -1;
        unsafe
        {
            var size = (nuint) sizeof(UniformBufferObject);
            var data = uniformBuffersMapped[currentImage];
            Unsafe.CopyBlock(data, &ubo, (uint)size);
        }


    }
}