using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static Stack<Action> CleanupStack = new();
    public static unsafe void CleanUp()
    {
        while (CleanupStack.TryPop(out var action))
        {
            action();
        }
        CleanupBufferImmediately(GPUDynamicBuffer.stagingBuffer, GPUDynamicBuffer.stagingMemory);
        imGuiController.Dispose();
        FreeGlobalData();
        vkDestroyBuffer(device, GlobalData.vertexBuffer, null);
        vkFreeMemory(device, GlobalData.vertexBuffer, null);
        CleanUpSwapChainStuff();
        
        vkDestroySampler(device, textureSampler, null);
        vkDestroyImageView(device, textureImageView, null);
        vkDestroyImage(device, textureImage, null);
        vkFreeMemory(device, textureImageMemory, null);
        
        for (var i = 0; i < FRAME_OVERLAP; i++)
        {
            FrameCleanup[i]();
        }

        vkDestroyDescriptorPool(device, DescriptorPool, null);
        vkDestroyDescriptorSetLayout(device, DescriptorSetLayout, null);
        
        vkDestroyBuffer(device, GlobalData.indexBuffer, null);
        vkFreeMemory(device, GlobalData.indexBuffer, null);



      


        vkDestroyDevice(device, null);

        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivilant to method DestroyDebugUtilsMessengerEXT from original tutorial.
            vkDestroyDebugUtilsMessengerEXT(instance, debugMessenger, null);
        }

        vkDestroySurfaceKHR(instance, mainWindow.surface, null);
        vkDestroyInstance(instance, null);

        VKRender.mainWindow.window.Dispose();
    }

    private static unsafe void CleanUpSwapChainStuff()
    {
        foreach (var image in mainWindow.SwapChainImages)
        {
            image.DestroyImmediate();
        }
        // for (int i = 0; i < FRAME_OVERLAP; i++)
        // {
        //     fixed (FrameData* frameData = &FrameData[i])
        //     {
        //         vkResetCommandPool(device,frameData->commandPool,0);
        //     }
        // }

        vkDestroyPipeline(device, GraphicsPipeline, null);
        vkDestroyPipelineLayout(device, GfxPipelineLayout, null);
        vkDestroyRenderPass(device, RenderPass, null);
        
        mainWindow.depthImage.DestroyImmediate();
        
        vkDestroySwapchainKHR(device, mainWindow.swapChain, null);
    }
}