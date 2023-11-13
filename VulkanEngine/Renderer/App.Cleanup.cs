using System.Diagnostics.CodeAnalysis;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static unsafe void CleanUp()
    {
        imGuiController.Dispose();
        
        vk.DestroyBuffer(device, GlobalData.VertexBuffer, null);
        vk.FreeMemory(device, GlobalData.VertexBufferMemory, null);
        CleanUpSwapChainStuff();
        
        vk.DestroySampler(device, textureSampler, null);
        vk.DestroyImageView(device, textureImageView, null);
        vk.DestroyImage(device, textureImage, null);
        vk.FreeMemory(device, textureImageMemory, null);
        
        for (var i = 0; i < FRAME_OVERLAP; i++) {
            vk.DestroyBuffer(device, uniformBuffers[i], null);
            vk.FreeMemory(device, uniformBuffersMemory[i], null);
        }

        vk.DestroyDescriptorPool(device, DescriptorPool, null);
        vk.DestroyDescriptorSetLayout(device, DescriptorSetLayout, null);
        
        vk.DestroyBuffer(device, IndexBuffer, null);
        vk.FreeMemory(device, IndexBufferMemory, null);



        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            vk.DestroySemaphore(device, FrameData[i].RenderSemaphore, null);
            vk.DestroySemaphore(device, FrameData[i].presentSemaphore, null);
            vk.DestroySemaphore(device, FrameData[i].transferSemaphore, null);
            vk.DestroyFence(device, FrameData[i].renderFence, null);
            vk.DestroyCommandPool(device, FrameData[i].commandPool, null);
        }


        vk.DestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }

        khrSurface!.DestroySurface(instance, surface, null);
        vk.DestroyInstance(instance, null);
        vk.Dispose();

        window?.Dispose();
    }

    private static unsafe void CleanUpSwapChainStuff()
    {
        foreach (var framebuffer in swapChainFramebuffers!)
        {
            vk.DestroyFramebuffer(device, framebuffer, null);
        }

        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            fixed (FrameData* frameData = &FrameData[i])
            {
                vk.ResetCommandPool(device,frameData->commandPool,0);
            }
        }

        vk.DestroyPipeline(device, GraphicsPipeline, null);
        vk.DestroyPipelineLayout(device, PipelineLayout, null);
        vk.DestroyRenderPass(device, RenderPass, null);
        
        vk.DestroyImage(device, GlobalData.depthImage,null);
        vk.FreeMemory(device, GlobalData.depthImageMemory,null);
        vk.DestroyImageView(device, GlobalData.depthImageView,null);
        
        foreach (var imageView in swapChainImageViews!)
        {
            vk.DestroyImageView(device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(device, swapChain, null);
    }
}