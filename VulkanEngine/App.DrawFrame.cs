using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine;

public static partial class App
{
    static Semaphore[] imageAvailableSemaphores;
    static Semaphore[] renderFinishedSemaphores;
    static Fence[] inFlightFences;
    private static unsafe void DrawFrame()
    {
        var fence= inFlightFences[CurrentFrameIndex];
        vk!.WaitForFences(logicalDevice, 1,  fence, true, ulong.MaxValue)
            .Expect("failed to wait for fence!");
        vk!.ResetFences(logicalDevice, 1, fence);
        uint imageIndex = 999;
        khrSwapChain.AcquireNextImage(logicalDevice, swapChain, ulong.MaxValue, imageAvailableSemaphores[CurrentFrameIndex], default, &imageIndex)
            .Expect("failed to acquire swap chain image!");
        vk!.ResetCommandBuffer(CommandBuffers[CurrentFrameIndex], 0);
        RecordCommandBuffer(CommandBuffers[CurrentFrameIndex], imageIndex);
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
        khrSwapChain.QueuePresent(presentQueue, &presentinfo)
            .Expect("failed to present swap chain image!");
        
    }
}