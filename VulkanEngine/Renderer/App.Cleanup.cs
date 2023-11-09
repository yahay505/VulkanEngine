using System.Diagnostics.CodeAnalysis;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static unsafe void CleanUp()
    {
        vk.DestroyBuffer(device, VertexBuffer, null);
        vk.FreeMemory(device, VertexBufferMemory, null);
        CleanUpSwapChainStuff();
        
        vk.DestroySampler(device, textureSampler, null);
        vk.DestroyImageView(device, textureImageView, null);
        vk.DestroyImage(device, textureImage, null);
        vk.FreeMemory(device, textureImageMemory, null);
        
        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++) {
            vk.DestroyBuffer(device, uniformBuffers[i], null);
            vk.FreeMemory(device, uniformBuffersMemory[i], null);
        }

        vk.DestroyDescriptorPool(device, DescriptorPool, null);
        vk.DestroyDescriptorSetLayout(device, DescriptorSetLayout, null);
        
        vk.DestroyBuffer(device, IndexBuffer, null);
        vk.FreeMemory(device, IndexBufferMemory, null);



        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk.DestroySemaphore(device, renderFinishedSemaphores[i], null);
            vk.DestroySemaphore(device, imageAvailableSemaphores[i], null);
            vk.DestroyFence(device, inFlightFences[i], null);
        }

        vk.DestroyCommandPool(device, CommandPool, null);

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

        fixed (CommandBuffer* commandBuffersPtr = CommandBuffers)
        {
            vk.FreeCommandBuffers(device, CommandPool, (uint) CommandBuffers.Length, commandBuffersPtr);
        }

        vk.DestroyPipeline(device, GraphicsPipeline, null);
        vk.DestroyPipelineLayout(device, PipelineLayout, null);
        vk.DestroyRenderPass(device, RenderPass, null);
        
        vk.DestroyImage(device,depthImage,null);
        vk.FreeMemory(device,depthImageMemory,null);
        vk.DestroyImageView(device,depthImageView,null);
        
        foreach (var imageView in swapChainImageViews!)
        {
            vk.DestroyImageView(device, imageView, null);
        }

        khrSwapChain!.DestroySwapchain(device, swapChain, null);
    }
}