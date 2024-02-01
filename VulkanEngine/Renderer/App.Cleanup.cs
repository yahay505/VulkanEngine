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
        vk.DestroyBuffer(device, GlobalData.vertexBuffer, null);
        vk.FreeMemory(device, GlobalData.vertexBuffer, null);
        CleanUpSwapChainStuff();
        
        vk.DestroySampler(device, textureSampler, null);
        vk.DestroyImageView(device, textureImageView, null);
        vk.DestroyImage(device, textureImage, null);
        vk.FreeMemory(device, textureImageMemory, null);
        
        for (var i = 0; i < FRAME_OVERLAP; i++)
        {
            FrameCleanup[i]();
        }

        vk.DestroyDescriptorPool(device, DescriptorPool, null);
        vk.DestroyDescriptorSetLayout(device, DescriptorSetLayout, null);
        
        vk.DestroyBuffer(device, GlobalData.indexBuffer, null);
        vk.FreeMemory(device, GlobalData.indexBuffer, null);



      


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

        // for (int i = 0; i < FRAME_OVERLAP; i++)
        // {
        //     fixed (FrameData* frameData = &FrameData[i])
        //     {
        //         vk.ResetCommandPool(device,frameData->commandPool,0);
        //     }
        // }

        vk.DestroyPipeline(device, GraphicsPipeline, null);
        vk.DestroyPipelineLayout(device, GfxPipelineLayout, null);
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