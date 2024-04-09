using Pastel;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static SwapChainSupportDetails deviceSwapChainSupport;

    private struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    private static unsafe SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice physicalDevice)
    {
        var details = new SwapChainSupportDetails();

        khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

        uint formatCount = 0;
        khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, null);

        if (formatCount != 0)
        {
            details.Formats = new SurfaceFormatKHR[formatCount];
            fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
            {
                khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, surface, ref formatCount, formatsPtr);
            }
        }
        else
        {
            details.Formats = Array.Empty<SurfaceFormatKHR>();
        }

        uint presentModeCount = 0;
        khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount, null);

        if (presentModeCount != 0)
        {
            details.PresentModes = new PresentModeKHR[presentModeCount];
            fixed (PresentModeKHR* formatsPtr = details.PresentModes)
            {
                khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, surface, ref presentModeCount,
                    formatsPtr);
            }
        }
        else
        {
            details.PresentModes = Array.Empty<PresentModeKHR>();
        }

        return details;
    }

    private static SurfaceFormatKHR ChooseSwapSurfaceFormat(IReadOnlyList<SurfaceFormatKHR> availableFormats)
    {
        foreach (var availableFormat in availableFormats)
        {
            if (availableFormat.Format == Format.B8G8R8A8Srgb &&
                availableFormat.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }
    private static PresentModeKHR ChoosePresentMode(IReadOnlyList<PresentModeKHR> availablePresentModes)
    {
        foreach (var availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == PresentModeKHR.MailboxKhr)
            {
                return availablePresentMode;
            }
        }

        return PresentModeKHR.FifoKhr;
    }
    
    private static Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return capabilities.CurrentExtent;
        }
        else
        {
            var framebufferSize = window!.FramebufferSize;

            Extent2D actualExtent = new()
            {
                Width = (uint)framebufferSize.X,
                Height = (uint)framebufferSize.Y
            };

            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);

            return actualExtent;
        }
    }

    private static unsafe void CreateSwapChain(EngineWindow window)
    {
        deviceSwapChainSupport = QuerySwapChainSupport(physicalDevice);
        window.surfaceFormat = ChooseSwapSurfaceFormat(deviceSwapChainSupport.Formats);
        window.presentMode = ChoosePresentMode(deviceSwapChainSupport.PresentModes);
        window.swapChainExtent = ChooseSwapExtent(deviceSwapChainSupport.Capabilities);

        var imageCount = deviceSwapChainSupport.Capabilities.MinImageCount + 1;
        if (deviceSwapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > deviceSwapChainSupport.Capabilities.MaxImageCount)
        {
            imageCount = deviceSwapChainSupport.Capabilities.MaxImageCount;
        }

        SwapchainCreateInfoKHR creatInfo = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = window.surface,

            MinImageCount = imageCount,
            ImageFormat = window.surfaceFormat.Format,
            ImageColorSpace = window.surfaceFormat.ColorSpace,
            ImageExtent = window.swapChainExtent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
        };

        var indices = DeviceInfo.indices;
        var queueFamilyIndices = stackalloc[] { indices.graphicsFamily!.Value, indices.presentFamily!.Value };

        if (indices.graphicsFamily != indices.presentFamily)
        {
            creatInfo = creatInfo with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            creatInfo.ImageSharingMode = SharingMode.Exclusive;
        }

        CompositeAlphaFlagsKHR compositeMode;
        if (window.transparency)
        {
            var alphaSupport = deviceSwapChainSupport.Capabilities.SupportedCompositeAlpha;
            if ((alphaSupport & (CompositeAlphaFlagsKHR.PostMultipliedBitKhr |
                                                        CompositeAlphaFlagsKHR.PreMultipliedBitKhr)) == 0)
            {
                throw new NotSupportedException("CompositeAlphaFlagsKHR.PostMultipliedBitKhr or CompositeAlphaFlagsKHR.PreMultipliedBitKhr not supported. yet transparency is requested");
            }

            compositeMode = alphaSupport.HasFlag(CompositeAlphaFlagsKHR.PreMultipliedBitKhr) ? CompositeAlphaFlagsKHR.PreMultipliedBitKhr : CompositeAlphaFlagsKHR.PostMultipliedBitKhr;
        }
        else
        {
            compositeMode = CompositeAlphaFlagsKHR.OpaqueBitKhr;
        }

        
        
        creatInfo = creatInfo with
        {
            PreTransform = deviceSwapChainSupport.Capabilities.CurrentTransform,
            // opaque if not needed, premultiplied if supported, else postmultiplied 
            CompositeAlpha = compositeMode,
            PresentMode = window.presentMode,
            Clipped = true,

            OldSwapchain = default //todo pass in old swapchain
        };

        Console.WriteLine($"created swapchain with compositeMode: {compositeMode} and presentMode: {window.presentMode}".Pastel(ConsoleColor.Green));

        if (!vk.TryGetDeviceExtension(instance, device, out window.khrSwapChain))
        {
            throw new NotSupportedException("VK_KHR_swapchain extension not found.");
        }
        
        if (khrSwapChain!.CreateSwapchain(device, creatInfo, null, out window.swapChain) != Result.Success)
        {
            throw new Exception("failed to create swap chain!");
        }

        khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, null);
        
        window.swapChainImages = new Silk.NET.Vulkan.Image[imageCount];
        fixed (Silk.NET.Vulkan.Image* swapChainImagesPtr = window.swapChainImages)
        {
            khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, swapChainImagesPtr);
        }

        window.swapChainImageFormat = window.surfaceFormat.Format;
        
    
    }

    private static void RecreateSwapChain(EngineWindow window)
    {
        // swapchain,imageviews,renderpass,depth,pipeline,framebuffer
        unsafe
        {
            var framebufferSize = window.window!.FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = window.window.FramebufferSize;
                window.window.DoEvents();
            }

            vk.DeviceWaitIdle(device);

            CleanUpSwapChainStuff(window);

            CreateSwapChain(window);
            CreateSwapChainImageViews(window);
            
            CreateRenderPass();
            CreateDepthResources();
            
            CreateGraphicsPipeline();
            CreateSwapchainFrameBuffers();
            // var allocInfo = new CommandBufferAllocateInfo
            // {
            //     SType = StructureType.CommandBufferAllocateInfo,
            //     // CommandPool = GetCurrentFrame().commandPool,
            //     Level = CommandBufferLevel.Primary,
            //     CommandBufferCount = 1,
            // };
            // for (var i = 0; i < FRAME_OVERLAP; i++)
            //     fixed(FrameData* frameData = &FrameData[i])
            //         vk.AllocateCommandBuffers(device, allocInfo with{CommandPool = frameData->commandPool},out frameData->mainCommandBuffer)
            //             .Expect("failed to allocate command buffers!");
            //

        }
    }
}