using Pastel;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static partial class Infra
{
    public static SwapChainSupportDetails deviceSwapChainSupport;

    public struct SwapChainSupportDetails
    {
        public VkSurfaceCapabilitiesKHR Capabilities;
        public VkSurfaceFormatKHR[] Formats;
        public VkPresentModeKHR[] PresentModes;
    }

    public static unsafe SwapChainSupportDetails QuerySwapChainSupport(VkPhysicalDevice physicalDevice,
        VkSurfaceKHR surface)
    {
        deviceSwapChainSupport = new();
        
        
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out deviceSwapChainSupport.Capabilities);

        uint formatCount = 0;
        vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, & formatCount, null);

        if (formatCount != 0)
        {
            deviceSwapChainSupport.Formats = new VkSurfaceFormatKHR[formatCount];
            fixed (VkSurfaceFormatKHR* formatsPtr = deviceSwapChainSupport.Formats)
            {
                vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, & formatCount, formatsPtr);
            }
        }
        else
        {
            deviceSwapChainSupport.Formats = Array.Empty<VkSurfaceFormatKHR>();
        }

        uint presentModeCount = 0;
        vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, & presentModeCount, null);

        if (presentModeCount != 0)
        {
            deviceSwapChainSupport.PresentModes = new VkPresentModeKHR[presentModeCount];
            fixed (VkPresentModeKHR* formatsPtr = deviceSwapChainSupport.PresentModes)
            {
                vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, & presentModeCount,
                    formatsPtr);
            }
        }
        else
        {
            deviceSwapChainSupport.PresentModes = Array.Empty<VkPresentModeKHR>();
        }

        return deviceSwapChainSupport;
    }

    public static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<VkSurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.format == VkFormat.B8G8R8A8Srgb &&
                availableFormat.colorSpace == VkColorSpaceKHR.SrgbNonLinear)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }
    public static VkPresentModeKHR ChoosePresentMode(IReadOnlyList<VkPresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == VkPresentModeKHR.Mailbox)
            {
                return availablePresentMode;
            }
        }

        return VkPresentModeKHR.Fifo;
    }
    
    public static VkExtent2D ChooseSwapExtent(EngineWindow window)
    {
        VkSurfaceCapabilitiesKHR capabilities;
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(API.physicalDevice, window.surface, out capabilities);
        if (Math.Max(capabilities.currentExtent.width,capabilities.currentExtent.height) != uint.MaxValue && Math.Min(capabilities.currentExtent.width,capabilities.currentExtent.height)!=0)
        {
            return capabilities.currentExtent;
        }
        else
        {
            Console.WriteLine("swapextent WTF???".Pastel(ConsoleColor.Magenta));
            
            throw new Exception("swapextent WTF???");
            // var framebufferSize = window.window!.FramebufferSize;

            // VkExtent2D actualExtent = new()
            // {
                // width = (uint)framebufferSize.X,
                // height = (uint)framebufferSize.Y
            // };
            //
            // actualExtent.width = Math.Clamp(actualExtent.width, capabilities.minImageExtent.width, capabilities.maxImageExtent.width);
            // actualExtent.height = Math.Clamp(actualExtent.height, capabilities.minImageExtent.height, capabilities.maxImageExtent.height);
            //
            // return actualExtent;
        }
    }
    //
    // private static unsafe void CreateSwapChain(EngineWindow window, VkSwapchainKHR oldSwapchain = default)
    // {
    //     deviceSwapChainSupport = QuerySwapChainSupport(API.physicalDevice,window.surface);
    //     window.surfaceFormat = ChooseSwapSurfaceFormat(deviceSwapChainSupport.Formats);
    //     window.presentMode = ChoosePresentMode(deviceSwapChainSupport.PresentModes);
    //     window.swapChainExtent = ChooseSwapExtent(window);
    //
    //     var imageCount = deviceSwapChainSupport.Capabilities.MinImageCount + 1;
    //     if (deviceSwapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > deviceSwapChainSupport.Capabilities.MaxImageCount)
    //     {
    //         imageCount = deviceSwapChainSupport.Capabilities.MaxImageCount;
    //     }
    //
    //     SwapchainCreateInfoKHR creatInfo = new()
    //    {
    //         Surface = window.surface,
    //
    //         MinImageCount = imageCount,
    //         ImageFormat = window.surfaceFormat.Format,
    //         ImageColorSpace = window.surfaceFormat.ColorSpace,
    //         ImageExtent = window.swapChainExtent,
    //         ImageArrayLayers = 1,
    //         ImageUsage = ImageUsageFlags.ColorAttachment,
    //     };
    //
    //     var indices = DeviceInfo.indices;
    //     var queueFamilyIndices = stackalloc[] { indices.graphicsFamily!.Value, indices.presentFamily!.Value };
    //
    //     if (indices.graphicsFamily != indices.presentFamily)
    //     {
    //         creatInfo = creatInfo with
    //         {
    //             ImageSharingMode = SharingMode.Concurrent,
    //             QueueFamilyIndexCount = 2,
    //             PQueueFamilyIndices = queueFamilyIndices,
    //         };
    //     }
    //     else
    //     {
    //         creatInfo.ImageSharingMode = SharingMode.Exclusive;
    //     }
    //
    //     CompositeAlphaFlagsKHR compositeMode;
    //     if (window.transparency)
    //     {
    //         var alphaSupport = deviceSwapChainSupport.Capabilities.SupportedCompositeAlpha;
    //         if ((alphaSupport & (CompositeAlphaFlagsKHR.PostMultipliedBitKhr |
    //                                                     CompositeAlphaFlagsKHR.PreMultipliedBitKhr)) == 0)
    //         {
    //             throw new NotSupportedException("CompositeAlphaFlagsKHR.PostMultipliedBitKhr or CompositeAlphaFlagsKHR.PreMultipliedBitKhr not supported. yet transparency is requested");
    //         }
    //
    //         compositeMode = alphaSupport.HasFlag(CompositeAlphaFlagsKHR.PreMultipliedBitKhr) ? CompositeAlphaFlagsKHR.PreMultipliedBitKhr : CompositeAlphaFlagsKHR.PostMultipliedBitKhr;
    //     }
    //     else
    //     {
    //         compositeMode = CompositeAlphaFlagsKHR.OpaqueBitKhr;
    //     }
    //
    //     
    //     
    //     creatInfo = creatInfo with
    //     {
    //         PreTransform = deviceSwapChainSupport.Capabilities.CurrentTransform,
    //         // opaque if not needed, premultiplied if supported, else postmultiplied 
    //         CompositeAlpha = compositeMode,
    //         PresentMode = window.presentMode,
    //         Clipped = true,
    //
    //         OldSwapchain = default //todo pass in old swapchain
    //     };
    //
    //     Console.WriteLine($"created swapchain with compositeMode: {compositeMode} and presentMode: {window.presentMode}".Pastel(ConsoleColor.Green));
    //
    //     if (!vkTryGetDeviceExtension(instance, device, out window.khrSwapChain))
    //     {
    //         throw new NotSupportedException("VK_KHR_swapchain extension not found.");
    //     }
    //     
    //     if (khrSwapChain!.CreateSwapchain(device, creatInfo, null, out window.swapChain) != Result.Success)
    //     {
    //         throw new Exception("failed to create swap chain!");
    //     }
    //
    //     khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, null);
    //     
    //     window.swapChainImages = new Silk.NET.Vulkan.Image[imageCount];
    //     fixed (Silk.NET.Vulkan.Image* swapChainImagesPtr = window.swapChainImages)
    //     {
    //         khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, swapChainImagesPtr);
    //     }
    //
    //     window.swapChainImageFormat = window.surfaceFormat.Format;
    //     
    //
    // }
    //
    // private static void RecreateSwapChain(EngineWindow window)
    // {
    //     // swapchain,imageviews,renderpass,depth,pipeline,framebuffer
    //     unsafe
    //     {
    //         var framebufferSize = window.window!.FramebufferSize;
    //
    //         while (framebufferSize.X == 0 || framebufferSize.Y == 0)
    //         {
    //             framebufferSize = window.window.FramebufferSize;
    //             window.window.DoEvents();
    //         }
    //
    //         vkDeviceWaitIdle(device);
    //
    //         CleanUpSwapChainStuff(window);
    //
    //         CreateSwapChain(window);
    //         CreateSwapChainImageViews(window);
    //         
    //         CreateRenderPass();
    //         CreateDepthResources();
    //         
    //         CreateGraphicsPipeline();
    //         CreateSwapchainFrameBuffers();
    //         // var allocInfo = new CommandBufferAllocateInfo
    //         //{
    //         //     // CommandPool = GetCurrentFrame().commandPool,
    //         //     Level = CommandBufferLevel.Primary,
    //         //     CommandBufferCount = 1,
    //         // };
    //         // for (var i = 0; i < FRAME_OVERLAP; i++)
    //         //     fixed(FrameData* frameData = &FrameData[i])
    //         //         vkAllocateCommandBuffers(device, allocInfo with{CommandPool = frameData->commandPool},out frameData->mainCommandBuffer)
    //         //             .Expect("failed to allocate command buffers!");
    //         //
    //
    //     }
    // }
}