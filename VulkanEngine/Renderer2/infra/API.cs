using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OSBindingTMP;
using Pastel;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Vulkan;
using VulkanEngine.Renderer;
using VulkanEngine.Renderer2.infra.Bindless;

using WindowsBindings;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static class API
{
    public static VkInstance instance;
    public static VkPhysicalDevice physicalDevice=>chosenDevice.device;
    public static VkDevice device;
    public static DeviceInfo chosenDevice;
    public static VkQueue graphicsQueue;
    public static VkQueue presentQueue;
    public static VkQueue computeQueue;
    public static VkQueue transferQueue;

    public static unsafe void InitVulkan()
    {
        Infra.CreateVkInstance();
        var boot_window = CreateWindow(new(960, 540), "boot",transparency:true,position:new(480,270));
        // boot_window.transparency = true;

        chosenDevice = DeviceRequirements.PickPhysicalDevice(boot_window.surface);
        Infra.CreateLogicalDevice(chosenDevice);
        vkCreateCommandPool(device, VkCommandPoolCreateFlags.None, chosenDevice.indices.graphicsFamily!.Value,
            out var commandPool);
        vkAllocateCommandBuffer(device, commandPool, VkCommandBufferLevel.Primary, out var boot_cb);
        CreateSwapchain(boot_window);
        TextureManager.InitTextureEngine();
        MaterialManager.Init();
        funnyRenderer.init(boot_window);
        while (true)
        {
            funnyRenderer.Render(boot_window);
            MacBinding.pump_messages(&message_loop, true);
        }
        // while (true)WinAPI.pump_messages(true);
    }
    #region window


    public static EngineWindow CreateWindow(
        int2 size,
        string title,
        bool vsync = true,
        bool transparency = false,
        object? windowBorder = null,
        int2? position = null,
        bool preferMailBox = false
    )
    {
        // var options = WindowOptions.DefaultVulkan with
        // {
        //     Size = size,
        //     Title = title,
        //     VSync = vsync,
        //     TransparentFramebuffer = transparency,
        //     WindowBorder = windowBorder??WindowOptions.DefaultVulkan.WindowBorder,
        //     Position = position??WindowOptions.DefaultVulkan.Position,
        // };
        
        return CreateWindowRaw();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void message_loop(InputEventStruct* input)
    {
        switch (input->type)
        {
            case InputEventStruct.KEYBOARD_EVENT:
                break;
            case InputEventStruct.MOUSE_EVENT:
                break;
            case InputEventStruct.WINDOW_EVENT:
                var data = *(InputEventStruct.WindowEvent.ResizeEvent*)input->window.data;
                windows[input->window.windowID].size = new(data.w, data.h);
                break;
        }
    }

    public static Dictionary<long, EngineWindow> windows = new();
    public static unsafe EngineWindow CreateWindowRaw(
    )
    {
        var raw = new EngineWindow();
        switch (MIT.OS)
        {
            case OSType.Mac:
            {
            var app = MacBinding.create_application();
            var window = MacBinding.open_window("Test",
                800,
                600,
                0,
                0,
                MacBinding.NSWindowStyleMask.NSWindowStyleMaskTitled
                | MacBinding.NSWindowStyleMask.NSWindowStyleMaskMiniaturizable
                | MacBinding.NSWindowStyleMask.NSWindowStyleMaskResizable
                |MacBinding.NSWindowStyleMask.NSWindowStyleMaskClosable
            );
            //MacBinding.set_transparent(window,1);
            var surface_ptr = MacBinding.window_create_surface(window);
            MacBinding.window_makeKeyAndOrderFront(window);
            raw.macwindow = window;
            raw.transparency = true;
            
            var macOsSurfaceCreateInfoMvk = new VkMetalSurfaceCreateInfoEXT()
            {
                flags = VkMetalSurfaceCreateFlagsEXT.None,
                pLayer = surface_ptr,
            };
            vkCreateMetalSurfaceEXT(instance, &macOsSurfaceCreateInfoMvk, null, out var surface).Expect();
            
            raw.surface = surface;
            windows.Add(((long) window.ptr)!,raw);
            
            MacBinding.pump_messages(&message_loop,true);

            }
                break;
            case OSType.Windows:
            {

                WinAPI.create_app();
                raw.HWND = WinAPI.open_window();
                raw.HINSTANCE = WinAPI.get_hinstance();
                var winSurfaceCreateInfo = new VkWin32SurfaceCreateInfoKHR()
                {
                    hinstance = raw.HINSTANCE,
                    hwnd = raw.HWND,
                };
                VkSurfaceKHR surface;
                vkCreateWin32SurfaceKHR(instance, &winSurfaceCreateInfo, null, &surface).Expect();
                raw.surface = surface;
                
            }
                break;
        }

        
    


        
        
        
        // if (raw.window.VkSurface is null)
        // {
        //     throw new Exception("Windowing platform doesn't support Vulkan.");
        // }
        
        return raw;
    }

    
    public static unsafe void CreateSwapchain(EngineWindow window,
        VkSwapchainKHR oldSwapchain = default)
    {
        bool preferMailbox = window.preferMailbox;
        
        var deviceSwapChainSupport = Infra.QuerySwapChainSupport(physicalDevice,window.surface);
        window.surfaceFormat = Infra.ChooseSwapSurfaceFormat(deviceSwapChainSupport.Formats); //this can dynamicly change
        window.presentMode = Infra.ChoosePresentMode(deviceSwapChainSupport.PresentModes);
        window.size = Infra.ChooseSwapExtent(window);

        window.swapChainImageFormat = window.surfaceFormat.format;


        var imageCount = deviceSwapChainSupport.Capabilities.minImageCount + 1;
        if (deviceSwapChainSupport.Capabilities.maxImageCount > 0 &&
            imageCount > deviceSwapChainSupport.Capabilities.maxImageCount)
        {
            imageCount = deviceSwapChainSupport.Capabilities.maxImageCount;
        }

        VkSwapchainPresentScalingCreateInfoEXT pnext;
        var creatInfo = new VkSwapchainCreateInfoKHR
        {
            surface = window.surface,
            minImageCount = imageCount,
            imageFormat = window.surfaceFormat.format,
            imageColorSpace = window.surfaceFormat.colorSpace,
            imageExtent = window.size,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.TransferDst|VkImageUsageFlags.ColorAttachment,
            flags = VkSwapchainCreateFlagsKHR.DeferredMemoryAllocationEXT,
            pNext = &pnext,
        };
        pnext = new()
        {
            presentGravityX = VkPresentGravityFlagsEXT.Min,
            presentGravityY = VkPresentGravityFlagsEXT.Min,
            scalingBehavior = VkPresentScalingFlagsEXT.OneToOne,
        };

        var queueFamilyIndices = stackalloc[] {chosenDevice.indices.graphicsFamily!.Value, chosenDevice.indices.presentFamily!.Value};

        window.swapchainImagesShared = chosenDevice.indices.graphicsFamily != chosenDevice.indices.presentFamily;
        if (window.swapchainImagesShared)
        {
            creatInfo = creatInfo with
            {
                imageSharingMode = VkSharingMode.Concurrent,
                queueFamilyIndexCount = 2,
                pQueueFamilyIndices = queueFamilyIndices,
            };
            throw new NotImplementedException(); // actually just throw here I dont want to work on this much
        }
        else
        {
            creatInfo.imageSharingMode = VkSharingMode.Exclusive;
        }

        VkCompositeAlphaFlagsKHR compositeMode;
        if (window.transparency)
        {
            var alphaSupport = deviceSwapChainSupport.Capabilities.supportedCompositeAlpha;
                
            if ((alphaSupport & (VkCompositeAlphaFlagsKHR.PostMultiplied | VkCompositeAlphaFlagsKHR.PreMultiplied)) == 0)
                throw new NotSupportedException("CompositeAlphaFlagsKHR.PostMultipliedBitKhr or CompositeAlphaFlagsKHR.PreMultipliedBitKhr not supported. yet transparency is requested");
                
            compositeMode = alphaSupport.HasFlag(VkCompositeAlphaFlagsKHR.PreMultiplied)
                ? VkCompositeAlphaFlagsKHR.PreMultiplied
                : VkCompositeAlphaFlagsKHR.PostMultiplied;
        }
        else
        {
            compositeMode = VkCompositeAlphaFlagsKHR.Opaque;
        }

        // compositeMode = VkCompositeAlphaFlagsKHR.PreMultiplied;
        window.composeAlpha = compositeMode;
        creatInfo = creatInfo with
        {
            preTransform = deviceSwapChainSupport.Capabilities.currentTransform,
            // opaque if not needed, premultiplied if supported, else postmultiplied 
            compositeAlpha = compositeMode,
            presentMode = window.presentMode,
            clipped = true,
            oldSwapchain = oldSwapchain
        };

        Console.WriteLine(
            $"created swapchain with compositeMode: {compositeMode} and presentMode: {window.presentMode}".Pastel(
                ConsoleColor.Green));

        vkCreateSwapchainKHR(device, &creatInfo, null, out window.swapChain)
            .Expect("failed to create swap chain!");


        vkGetSwapchainImagesKHR(device, window.swapChain, & imageCount, null).Expect();
        if (window.CleanupQueue!=null)
        {
            if ((window.SwapChainImages?.Length ?? -1)!= imageCount)
            {
                vkDeviceWaitIdle(device);
                foreach (var action in window.CleanupQueue) action();
                window.CleanupQueue = new Action[imageCount];
            }
        } else
        {
            
            window.CleanupQueue = new Action[imageCount];
            
            for (var i = 0; i < imageCount; i++)
            {
                var i1 = i;
                window.CleanupQueue[i] = () =>
                {
                    DestroyImage(window.SwapChainImages[i1]);
                };
            }
        }

        window.SwapChainImages = new EngineImage[imageCount];
        window.ReadyToPresentToSwapchainSemaphores = new VkSemaphore[imageCount];
        window.AcqforblitSemaphores = new VkSemaphore[imageCount];
        window.SwapchainSize = window.size;
        var tmp_swapchain = stackalloc VkImage[(int)imageCount];
        vkGetSwapchainImagesKHR(device, window.swapChain, & imageCount, tmp_swapchain);
            
        for (var i = 0; i < imageCount; i++)
            {
                window.SwapChainImages[i] = new EngineImage
                {
                    height = window.size.height,
                    width = window.size.width,
                    imageFormat = window.swapChainImageFormat,
                    deviceImage = tmp_swapchain[i],
                    // view = imageview,
                    hasMips = false,
                    mipCount = 1,
                    layout = [VkImageLayout.Undefined],
                    memory = default,
                    hostImage = null,
                    aspectFlags = VkImageAspectFlags.Color,
                };
                window.SwapChainImages[i].view = CreateAdditionalImageView(window.SwapChainImages[i], 0,1);

                vkCreateSemaphore(device, out window.ReadyToPresentToSwapchainSemaphores[i]);
                vkCreateSemaphore(device, out window.AcqforblitSemaphores[i]);
            }
    }

    // public static unsafe void ResizeSwapChain(EngineWindow window, int2 newsize)
    // {
    //     // we should not be touching in use items
    //     // vkDeviceWaitIdle(device);
    //     
    //     // queue swap chain image/view/depth/framebuff cleanup
    //     window.resizeFrameNo = CurrentFrame;
    //
    //     var a = new VkSwapchainCreateInfoKHR()
    //     {
    //         surface = window.surface,
    //         minImageCount = (uint) window.SwapChainImages.Length,
    //         imageFormat = window.surfaceFormat.format,
    //         imageColorSpace = window.surfaceFormat.colorSpace,
    //         imageExtent = new VkExtent2D((uint)newsize.X, (uint)newsize.Y),
    //         imageArrayLayers = 1,
    //         imageUsage = VkImageUsageFlags.ColorAttachment,
    //         
    //         
    //         preTransform = deviceSwapChainSupport.Capabilities.currentTransform,
    //         // opaque if not needed, premultiplied if supported, else postmultiplied 
    //         compositeAlpha = window.composeAlpha,
    //         presentMode = window.presentMode,
    //         clipped = true,
    //
    //
    //         oldSwapchain = window.swapChain 
    //     };
    //     
    //     var queueFamilyIndices = stackalloc[] {DeviceInfo.indices.graphicsFamily!.Value, DeviceInfo.indices.presentFamily!.Value};
    //
    //     if (window.swapchainImagesShared)
    //     {
    //         a = a with
    //         {
    //             imageSharingMode = VkSharingMode.Concurrent,
    //             queueFamilyIndexCount = 2,
    //             pQueueFamilyIndices = queueFamilyIndices,
    //         };
    //     }
    //     else
    //     {
    //         a.imageSharingMode = VkSharingMode.Exclusive;
    //     }
    //     vkCreateSwapchainKHR(device, &a,null,out var new_swapChain)
    //         .Expect();
    //     var imagecount = window.SwapChainImages.Length;
    //     var tmp_swapchain = stackalloc VkImage[(int)imagecount];
    //     vkGetSwapchainImagesKHR(device,new_swapChain,(uint*)&imagecount,tmp_swapchain)
    //         .Expect();
    //
    //     for (var i = 0; i < imagecount; i++)
    //     {
    //         window.SwapChainImages[i].EnqueueDestroy();
    //         window.SwapChainImages[i] = new ScreenSizedImage(
    //             newsize,
    //             window.swapChainImageFormat,
    //             false,
    //             tmp_swapchain[i], 
    //             default,
    //             default,
    //             default,
    //             CreateImageView(tmp_swapchain[i], window.swapChainImageFormat, VkImageAspectFlags.Color),
    //             CurrentFrame);
    //     }
    //     window.depthImage.EnqueueDestroy();
    //     window.depthImage = AllocateScreenSizedImage(newsize,window.depthImage.ImageFormat,window.depthImage.ImageUsage,window.depthImage.MemoryProperties, VkImageAspectFlags.Depth, false );
    //     TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal,0,1);
    //
    //     vkDestroySwapchainKHR(device,window.swapChain,null);
    //     window.swapChain = new_swapChain;
    //     window.size = new VkExtent2D((uint)newsize.X, (uint)newsize.Y);
    //     return;
    // }

    public static unsafe void present(EngineWindow window,EngineImage src, VkCommandBuffer blit_cb,VkSemaphore waitSemaphore,VkFence signalfence,Action window_resized_callback)
    {
        int retryCount = 0;
        window.presenterState = (window.presenterState + 1) % window.SwapChainImages.Length;
        window.CleanupQueue[window.presenterState]();
        window.CleanupQueue[window.presenterState] = () => {};
        
        acquire:
        uint index=uint.MaxValue;
        var rez = (window.SwapchainSize != window.size) ? VkResult.ErrorOutOfDateKHR:
            vkAcquireNextImageKHR(device,window.swapChain,UInt64.MaxValue,window.AcqforblitSemaphores[window.presenterState],default, out index);
        switch (rez)
        {
            case VkResult.Success:
                break;
            case VkResult.SuboptimalKHR:
                fixed(VkSemaphore* aa=window.AcqforblitSemaphores)
                {
                    var semwaitinfo = new VkSemaphoreWaitInfo()
                    {
                        semaphoreCount = 1,
                        pSemaphores = &aa[window.presenterState],
                    };
                    vkWaitSemaphores(device,&semwaitinfo,ulong.MaxValue);
                }
                var relinfo = new VkReleaseSwapchainImagesInfoEXT()
                {
                    swapchain = window.swapChain,
                    imageIndexCount = 1,
                    pImageIndices = &index,
                };
                vkReleaseSwapchainImagesEXT(device, &relinfo);
                goto case VkResult.ErrorOutOfDateKHR;
            case VkResult.ErrorOutOfDateKHR:
                retryCount++;
                if (retryCount>10)
                    throw new Exception();
                CreateSwapchain(window,window.swapChain);
                window_resized_callback();

                goto acquire;
                
            default: throw new Exception();
        }
        
        vkBeginCommandBuffer(blit_cb, VkCommandBufferUsageFlags.OneTimeSubmit);
        
        var windowSwapChainImage = window.SwapChainImages[index];

        TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.General, 0, 1);
        VkClearColorValue a = new(1f, 1f, 1f, 1f);
        VkImageSubresourceRange b = new()
        {
            aspectMask = VkImageAspectFlags.Color,
            layerCount = 1,
            levelCount = 1,
            baseArrayLayer = 0,
            baseMipLevel = 0,
        };
        vkCmdClearColorImage(blit_cb,windowSwapChainImage.deviceImage,VkImageLayout.General,&a,1,&b);
        TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.TransferDstOptimal, 0, 1);
        var blit = new VkImageBlit()
        {
            srcSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
            dstSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
        };
        blit.dstOffsets[0] = new(0, 0, 0);
        blit.dstOffsets[1] = new((int) window.SwapchainSize.width, (int) window.SwapchainSize.height, 1);
        blit.srcOffsets[0] = new(0, 0, 0);
        blit.srcOffsets[1] = new((int) src.width,(int) src.height,1);

        vkCmdBlitImage(blit_cb,src.deviceImage,src.layout[0],windowSwapChainImage.deviceImage,windowSwapChainImage.layout[0],1,&blit,VkFilter.Nearest);
        TransitionImageLayout(blit_cb, windowSwapChainImage, VkImageLayout.PresentSrcKHR, 0, 1);

        vkEndCommandBuffer(blit_cb);

        var vkPipelineStageFlags = VkPipelineStageFlags.AllCommands;
        var waits = stackalloc VkSemaphore[] {waitSemaphore,window.AcqforblitSemaphores[window.presenterState] };
        var signal = window.ReadyToPresentToSwapchainSemaphores[index];
        var blitSub = new VkSubmitInfo()
        {
            commandBufferCount = 1,
            pCommandBuffers = &blit_cb,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &signal,
            waitSemaphoreCount = 2,
            pWaitSemaphores = waits,
            pWaitDstStageMask = &vkPipelineStageFlags
        };
        vkQueueSubmit(graphicsQueue, blitSub, signalfence);

        vkQueuePresentKHR(graphicsQueue, signal, window.swapChain, index);
    }

    #endregion

    #region shader and pipeline

    public static unsafe VkShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var shaderCreateInfo = new VkShaderModuleCreateInfo()
            {
                codeSize = (nuint) code.Length,
                pCode = (uint*) pCode,
            };
            vkCreateShaderModule(device, &shaderCreateInfo, null, out var result)
                .Expect("failed to create shader module!");
            return result;
        }
    }

    public static unsafe VkPipelineLayout CreatePipelineLayout(ReadOnlySpan<VkDescriptorSetLayout> descriptorSetLayouts)
    {
        fixed(VkDescriptorSetLayout* descriptorSetLayoutsPtr = descriptorSetLayouts)
        {
            // create pipeline layout
            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {

                setLayoutCount = (uint) descriptorSetLayouts.Length,
                pSetLayouts = descriptorSetLayoutsPtr,
                pushConstantRangeCount = 0,
            };
            vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, out var pipelineLayout)
                .Expect("failed to create pipeline layout!");
            return pipelineLayout;
        }
    }
    public static VkPipeline CreatePSO(
            ReadOnlySpan<VkPipelineShaderStageCreateInfo> shaderStages,
            ReadOnlySpan<VkVertexInputBindingDescription> VertexBindings,
            ReadOnlySpan<VkVertexInputAttributeDescription> VertexDefinitions,
            VkPrimitiveTopology topology,
            bool primitiveRestartEnable,
            ReadOnlySpan<VkDynamicState> dynamicStates,
            VkPipelineRasterizationStateCreateInfo rasterizer,
            VkPipelineMultisampleStateCreateInfo multisampling,
            VkPipelineDepthStencilStateCreateInfo depthStencil,
            VkPipelineColorBlendStateCreateInfo colorBlending,
            VkPipelineLayout pipelineLayout,
            VkRenderPass renderPass,
            uint subpass
            )
    {
        // (dynamicStates.Gen().Count(a => a == VkDynamicState.Viewport) > 0)
        //     .Assert("Viewport must be in dynamic states");
        // (dynamicStates.Gen().Count(a => a == VkDynamicState.Scissor) > 0)
        //     .Assert("Scissor must be in dynamic states");
        unsafe
        {
            fixed(VkPipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
            fixed(VkDynamicState* dynamicStatesPtr = dynamicStates)
            fixed(VkVertexInputBindingDescription* VertexBindingsPtr = VertexBindings)
            fixed(VkVertexInputAttributeDescription* VertexDefinitionsPtr = VertexDefinitions)
            {
                
                //
                // create pipeline
                var dynamicStateCI = new VkPipelineDynamicStateCreateInfo
                {
                    dynamicStateCount = (uint) dynamicStates.Length,
                    pDynamicStates = dynamicStatesPtr,
                };
                var CBACI = new VkPipelineColorBlendStateCreateInfo
                {
                    
                };
                
                var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
                {
                    vertexBindingDescriptionCount = (uint) VertexBindings.Length,
                    pVertexBindingDescriptions = VertexBindingsPtr,
                    vertexAttributeDescriptionCount = (uint) VertexDefinitions.Length,
                    pVertexAttributeDescriptions = VertexDefinitionsPtr,
                };

                var inputAss = new VkPipelineInputAssemblyStateCreateInfo
                {
                    topology = topology,
                    primitiveRestartEnable = primitiveRestartEnable
                };
                var viewportState = new VkPipelineViewportStateCreateInfo
                {
                    viewportCount = 1,
                    scissorCount = 1,
                };
                var PSOCI = new VkGraphicsPipelineCreateInfo
                {
                    pNext = null,
                    stageCount = (uint) shaderStages.Length,
                    pStages = shaderStagesPtr,
                    pVertexInputState = &vertexInputInfo,
                    basePipelineHandle = default,
                    basePipelineIndex = default,
                    flags = default,
                    layout = pipelineLayout,
                    renderPass = renderPass,
                    subpass = subpass,
                    pColorBlendState = &colorBlending,
                    pDepthStencilState = &depthStencil,
                    pDynamicState = &dynamicStateCI,
                    pInputAssemblyState = &inputAss,
                    pTessellationState = null, // idk
                    pViewportState = &viewportState,
                    pRasterizationState = &rasterizer,
                    pMultisampleState = &multisampling,


                };
                vkCreateGraphicsPipeline(device, default, PSOCI, out var pipeline)
                    .Expect("failed to create graphics pipeline!");

                return pipeline;
            }    
        }
    }

    public static unsafe VkRenderPass CreateRenderPass(Span<VkAttachmentDescription> attachments,Span<VkSubpassDependency> deps,Span<VkAttachmentReference> refs, VkAttachmentReference depthStencil)
    {
        VkSubpassDescription subpass = new()
        {
            flags = VkSubpassDescriptionFlags.None,
            colorAttachmentCount = (uint) refs.Length,
            inputAttachmentCount = 0,
            pColorAttachments = refs.ptr(),
            pDepthStencilAttachment = &depthStencil,
            pInputAttachments = null,
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            
        };

        var rpci = new  VkRenderPassCreateInfo()
        {
            flags   = VkRenderPassCreateFlags.None,
            attachmentCount = (uint) attachments.Length,
            pAttachments = attachments.ptr(),
            pSubpasses = &subpass,
            subpassCount = 1,
            dependencyCount = (uint) deps.Length,
            pDependencies = deps.ptr(),
            
        };
        vkCreateRenderPass(device, &rpci, null, out var ret);
        return ret;
    }
    public static unsafe (VkPipeline pipeline, VkPipelineLayout pipelineLayout) CreateComputePSO(
        VkPipelineShaderStageCreateInfo shaderStage,
        Span<VkDescriptorSetLayout> descriptorSetLayouts
    )
    {
        fixed(VkDescriptorSetLayout* descriptorSetLayoutsPtr= descriptorSetLayouts)
        {
            var computePipelineLayoutInfo = new VkPipelineLayoutCreateInfo
           {
                setLayoutCount = (uint) descriptorSetLayouts.Length,
                pSetLayouts =  (descriptorSetLayoutsPtr),

            };
            vkCreatePipelineLayout(device, &computePipelineLayoutInfo, null, out var layout);
            var computePipelineInfo = new VkComputePipelineCreateInfo
           {
                stage = shaderStage,
                layout = layout,
            };
            VkPipeline pipeline;
            vkCreateComputePipelines(device, default, 1, &computePipelineInfo, null, & pipeline )
                .Expect("Failed to create compute pipeline!");
            return (pipeline, layout);
        }
    }

    #endregion

    #region memory

    
    public static unsafe void DestroyBuffer(VkBuffer buffer, VkDeviceMemory memory)
    {
        vkDestroyBuffer(device,buffer);
        vkFreeMemory(device,memory);
    }
    public static unsafe void CreateBufferMapped(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties,
        out VkBuffer buffer, out VkDeviceMemory bufferMemory, out void* ptr)
    {
        CreateBuffer(size, usage, properties, out buffer, out bufferMemory);
        void* a;
        vkMapMemory(device, bufferMemory, 0, size, VkMemoryMapFlags.None, &a);
        ptr = a;
    }
    public static unsafe void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties,
        out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        fixed (VkBuffer* pBuffer = &buffer)
        fixed (VkDeviceMemory* pBufferMemory = &bufferMemory)
            CreateBuffer(size, usage, properties, pBuffer, pBufferMemory);
    }
    public static unsafe void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, VkBuffer* buffer, VkDeviceMemory* bufferMemory)
    {
        var bufferInfo = new VkBufferCreateInfo
        {
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };
        vkCreateBuffer(device, &bufferInfo, null, buffer)
            .Expect("failed to create buffer!");
        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(device, *buffer, &memRequirements);
        
        var allocInfo = new VkMemoryAllocateInfo
        {
            allocationSize = memRequirements.size,
            memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties)
        };

        vkAllocateMemory(device, &allocInfo, null, bufferMemory) 
            .Expect("failed to allocate buffer memory!");
        
        vkBindBufferMemory(device, *buffer, *bufferMemory, 0).Expect();
    }
    
    private static unsafe uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        vkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);
        for (uint i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 && memProperties.memoryTypes[(int) i].propertyFlags.HasFlag(properties))
            {
                return i;
            }
        }
        throw new Exception("failed to find suitable memory type!");
    }

    #endregion

    #region assets
public static unsafe (DefaultVertex[] vertices,uint[] indices,float4x4 transform,int parentID)[] LoadMesh(string File)
    {
        var id = -1;
        using var assimp = Assimp.GetApi()!;
        
        var scene=assimp.ImportFile(File, (uint)PostProcessPreset.TargetRealTimeMaximumQuality)!;
        var a = new List<(DefaultVertex[], uint[], float4x4, int)>();
        VisitSceneNode(scene->MRootNode,id,Matrix4x4.Identity);
        
        assimp.ReleaseImport(scene);
        
        return a.ToArray();

        void VisitSceneNode(Node* node,int parentID,Matrix4x4 matrixStack)
        {
            Matrix4x4 ma = matrixStack;
            id++;
            if (node->MNumMeshes ==0)
            {
                ma = ma * node->MTransformation;
            }
            else
            {
                ma = Matrix4x4.Identity;
            }
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];
                var vertexMap = new Dictionary<DefaultVertex, uint>();
                var _vertices = new List<DefaultVertex>();
                var _indices = new List<uint>();

                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];
        
                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices![i];
        
                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords![0]![(int)index];
        
                        DefaultVertex vertex = new()
                        {
                            pos = new float3(position.X, position.Y, position.Z),
                            color = new Vector3D<float>(1, 1, 1),
                            //Flip Y for OBJ in Vulkan
                            texCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                        };
        
                        if (vertexMap.TryGetValue(vertex, out var meshIndex))
                        {
                            _indices.Add(meshIndex);
                        }
                        else
                        {
                            _indices.Add((uint)_vertices.Count);
                            vertexMap[vertex] = (uint)_vertices.Count;
                            _vertices.Add(vertex);
                        }
                    }
                }
                a.Add((_vertices.ToArray(), _indices.ToArray(), (ma * node->MTransformation).ToGeneric(),parentID));
            }
        
            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]!,id,ma);
            }
        }
    }
    public static string AssetsPath
    {
        get
        {
            if (_RPath != null) return _RPath;
            var f=Directory.GetParent(Assembly.GetExecutingAssembly().Location);
            while (f.GetDirectories("Assets").Length==0)
            {
                f=f.Parent!;
                if (f==null) throw new Exception("Assets folder not found");
            }
            _RPath=f.FullName+"/Assets/";
            return _RPath;
        }
    }


    private static string _RPath;


    #endregion

    #region images

    public static unsafe void DestroyImage(EngineImage img)
    {
        if(img.view.Handle!=default)vkDestroyImageView(device,img.view);
        if(img.memory.Handle!=default)vkDestroyImage(device,img.deviceImage);
        if(img.memory.Handle!=default)vkFreeMemory(device,img.memory);
        img.hostImage?.Dispose();
        
    }

    public static unsafe void AllocateDeviceResourcesForImage(EngineImage image)
    {
        
        //create image
        var imageCreateInfo = new VkImageCreateInfo
        {
            imageType = VkImageType.Image2D,
            format = image.imageFormat,
            extent = new VkExtent3D
            {
                width = image.width,
                height = image.height,
                depth = 1,
            },
            mipLevels = image.hasMips?MipCount(image.width,image.height):1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = image.tiling,
            usage = image.usage,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined,
            flags = image.flags,
        };
        vkCreateImage(device, &imageCreateInfo, null, out image.deviceImage)
            .Expect("failed to create image!");
    
        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(device, image.deviceImage, &memRequirements);

        var allocInfo = new VkMemoryAllocateInfo
        {
            allocationSize = memRequirements.size,
            memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, image.memProperties)
        };

        vkAllocateMemory(device, &allocInfo, null, out image.memory)
        .Expect("failed to allocate image memory!");

        vkBindImageMemory(device, image.deviceImage, image.memory, 0)
            .Expect();

        image.view = CreateAdditionalImageView(image, 0, image.hasMips ? image.mipCount : 1);
    }
    public static unsafe EngineImage CreateImage(uint width, uint height, VkFormat format, VkImageTiling tiling,
        VkImageUsageFlags usage, VkMemoryPropertyFlags properties, bool hasMips, VkImageCreateFlags ImageFlags, VkImageAspectFlags aspectFlags)
    {
        var img = new EngineImage(null)
        {
            width = width,
            height = height,
            hasMips = hasMips,
            imageFormat = format,
            layout = hasMips?Enumerable.Repeat(VkImageLayout.Undefined,(int) MipCount(width, height)).ToArray():[VkImageLayout.Undefined],
            mipCount = hasMips ? MipCount(width, height) : 1,
            aspectFlags = aspectFlags,
            tiling = tiling,
            usage = usage,
            memProperties = properties,
            flags = ImageFlags,
        };
        

        return img;
    }
    public static unsafe void CopyBufferToImage(VkCommandBuffer commandBuffer, VkBuffer buffer, VkImage image, uint width, uint height, uint mipLevel)
    {
        var region= new VkBufferImageCopy
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = mipLevel,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            imageOffset = new VkOffset3D
            {
                x = 0,
                y = 0,
                z = 0,
            },
            imageExtent = new VkExtent3D
            {
                width = width,
                height = height,
                depth = 1,
            }
        };
        vkCmdCopyBufferToImage(commandBuffer, buffer, image, VkImageLayout.TransferDstOptimal, 1, &region);
    }
/// <summary>
///  note: changes the layout of engineIMage as well but changes take effect on CB execution
/// </summary>
/// <param name="commandBuffer"></param>
/// <param name="image"></param>
/// <param name="newLayout"></param>
/// <param name="baseMipLevel"></param>
/// <param name="MiplevelCount"></param>
/// <exception cref="Exception"></exception>
    public static unsafe void TransitionImageLayout(VkCommandBuffer commandBuffer, EngineImage image, VkImageLayout newLayout, uint baseMipLevel, uint MiplevelCount)
    {
        var target = image.deviceImage;

        VkImageLayout oldLayout;
        for (var i = 0; i < MiplevelCount; i++)
        {
            var start = i;
            oldLayout = image.layout[baseMipLevel+i];

            while (i < MiplevelCount - 1 && image.layout[baseMipLevel + i + 1] == oldLayout) i++;
            TransitionImageLayoutRaw(commandBuffer, target, oldLayout, newLayout, image.imageFormat, (uint) (baseMipLevel+start), (uint) (i-start+1));
        }
        for (var i = 0; i < MiplevelCount; i++)
            image.layout[baseMipLevel+i] = newLayout;

    }

    public static unsafe void TransitionImageLayoutRaw(VkCommandBuffer commandBuffer, VkImage target, VkImageLayout oldLayout,
        VkImageLayout newLayout, VkFormat format, uint baseMipLevel, uint MiplevelCount)
    {
        var barrier= new VkImageMemoryBarrier
        {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = target,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = baseMipLevel,
                levelCount = MiplevelCount,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            dstAccessMask = 0,// TODO
            srcAccessMask = 0,// TODO
            
        };
        
        VkPipelineStageFlags sourceStage;
        VkPipelineStageFlags destinationStage;
        switch (oldLayout,newLayout)
        {
            case (VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal):
                barrier.srcAccessMask = 0;
                barrier.dstAccessMask = VkAccessFlags.TransferWrite;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.Transfer;
                break;
            case (VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal):
                barrier.srcAccessMask = VkAccessFlags.TransferWrite;
                barrier.dstAccessMask = VkAccessFlags.ShaderRead;
                sourceStage = VkPipelineStageFlags.Transfer;
                destinationStage = VkPipelineStageFlags.FragmentShader;
                break;
            case (VkImageLayout.Undefined, VkImageLayout.DepthAttachmentOptimal):
            case (VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal):
                barrier.srcAccessMask = 0;
                barrier.dstAccessMask = VkAccessFlags.DepthStencilAttachmentRead | VkAccessFlags.DepthStencilAttachmentWrite;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.EarlyFragmentTests;
                barrier.subresourceRange.aspectMask = VkImageAspectFlags.Depth; // migh tbe wrong
                break;
            case (VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal):
                barrier.srcAccessMask = VkAccessFlags.TransferWrite;
                barrier.dstAccessMask = VkAccessFlags.TransferRead;
                sourceStage = VkPipelineStageFlags.Transfer;
                destinationStage = VkPipelineStageFlags.Transfer;
                break;
            case (VkImageLayout.TransferSrcOptimal, VkImageLayout.ShaderReadOnlyOptimal):
                barrier.srcAccessMask = VkAccessFlags.TransferRead;
                barrier.dstAccessMask = VkAccessFlags.ShaderRead;
                sourceStage = VkPipelineStageFlags.Transfer;
                destinationStage = VkPipelineStageFlags.ComputeShader|VkPipelineStageFlags.AllGraphics;
                break;
            case (VkImageLayout.Undefined, VkImageLayout.ColorAttachmentOptimal):
                barrier.srcAccessMask = 0;
                barrier.dstAccessMask = VkAccessFlags.ColorAttachmentWrite;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.ColorAttachmentOutput;
                break;
            case (VkImageLayout.ColorAttachmentOptimal, VkImageLayout.TransferSrcOptimal):
                barrier.srcAccessMask = VkAccessFlags.ColorAttachmentWrite;
                barrier.dstAccessMask = VkAccessFlags.TransferRead;
                sourceStage = VkPipelineStageFlags.ColorAttachmentOutput;
                destinationStage = VkPipelineStageFlags.Transfer;
                break;
            case (VkImageLayout.TransferDstOptimal, VkImageLayout.PresentSrcKHR):
                barrier.srcAccessMask = VkAccessFlags.TransferWrite;
                barrier.dstAccessMask = VkAccessFlags.None;
                sourceStage = VkPipelineStageFlags.Transfer;
                destinationStage = VkPipelineStageFlags.BottomOfPipe;
                break;
            case (VkImageLayout.PresentSrcKHR, VkImageLayout.TransferDstOptimal):
                barrier.srcAccessMask = VkAccessFlags.None;
                barrier.dstAccessMask = VkAccessFlags.TransferWrite;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.Transfer;
                break;
            case (VkImageLayout.Undefined, VkImageLayout.TransferSrcOptimal):
                barrier.srcAccessMask = VkAccessFlags.None;
                barrier.dstAccessMask = VkAccessFlags.TransferRead;
                sourceStage = VkPipelineStageFlags.TopOfPipe;
                destinationStage = VkPipelineStageFlags.Transfer;
                break;
            default:
                break;       
                // throw new Exception("unsupported layout transition!");
        }
        
        if(newLayout==VkImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Depth;
            if (HasStencilComponent(format))
            {
                barrier.subresourceRange.aspectMask |= VkImageAspectFlags.Stencil;
            }
        }
        //debug
        barrier.dstAccessMask = (VkAccessFlags) (int.MaxValue>>1);
        barrier.srcAccessMask = (VkAccessFlags) (int.MaxValue>>1);
        sourceStage = VkPipelineStageFlags.AllCommands;
        destinationStage = VkPipelineStageFlags.AllCommands;
        
        
        vkCmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );
    }

    public static bool HasStencilComponent(VkFormat format)
    {
        switch (format)
        {
            case VkFormat.D32SfloatS8Uint:
            case VkFormat.D24UnormS8Uint:
            case VkFormat.S8Uint:
                return true;
            default:
                return false;
        }
    }
    public static bool HasDepthComponent(VkFormat format)
    {
        switch (format)
        {
            case VkFormat.D32SfloatS8Uint:
            case VkFormat.D32Sfloat:
            case VkFormat.D24UnormS8Uint:
            case VkFormat.D16Unorm:
            case VkFormat.D16UnormS8Uint:
                return true;
            default:
                return false;
        }
    }


    public static unsafe VkImageView CreateAdditionalImageView(EngineImage image,uint baseMip, uint mipCount)
    {
        var createInfo = new VkImageViewCreateInfo
        {
            image = image.deviceImage,
            viewType = VkImageViewType.Image2D,
            format = image.imageFormat,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            subresourceRange =
            {
                aspectMask = image.aspectFlags,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1,
            }
        };
        
        vkCreateImageView(device, &createInfo, null, out var imageView).Expect("failed to create image views!");
        return imageView;
    }

    public static VkFormat FindDepthFormat()
    {
        return FindSupportedFormat(new[] {  VkFormat.D32SfloatS8Uint, VkFormat.D32Sfloat, VkFormat.D24UnormS8Uint }, VkImageTiling.Optimal, VkFormatFeatureFlags.DepthStencilAttachment);
    }
    
    private static VkFormat FindSupportedFormat(VkFormat[] candidates, VkImageTiling tiling, VkFormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

            if (tiling == VkImageTiling.Linear && (props.linearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == VkImageTiling.Optimal && (props.optimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    #endregion

    [Pure]
    public static uint MipCount(uint width, uint height) => (uint) (BitOperations.Log2(Math.Max(width, height)) + 1);

    public static unsafe VkFramebuffer CreateFrameBuffer(uint witdh, uint height,Span<VkImageView> views, VkRenderPass renderPass)
    {
        
        var CI = new VkFramebufferCreateInfo()
        {
            flags = VkFramebufferCreateFlags.None,
            height = height,
            width = witdh,
            attachmentCount = (uint) views.Length,
            renderPass = renderPass,
            layers = 1,
            pAttachments = views.ptr(),
        };
        vkCreateFramebuffer(device, &CI, null, out var ret);
        return ret;
    }
}

public class EngineImage(string? path = null)
{
    public readonly string path = path?? ":mem:";
    // public GPUResourceStatus status;
    public Image<Rgba32>? hostImage;
    public VkImage deviceImage;
    public VkImageView view;
    public VkDeviceMemory memory;
    public VkImageLayout[] layout=[VkImageLayout.Undefined];
    public uint width;
    public uint height;
    public bool hasMips;
    public uint mipCount;
    public VkFormat imageFormat;
    public VkImageAspectFlags aspectFlags;
    public VkImageTiling tiling;
    public VkImageUsageFlags usage;
    public VkMemoryPropertyFlags memProperties;
    public VkImageCreateFlags flags;
}

public class EngineWindow
{
    public unsafe byte* debugName = "unnamed window"u8.GetPointer();
    public VkSurfaceKHR surface;
    public VkExtent2D size;
    public VkExtent2D SwapchainSize;
    public bool transparency,preferMailbox;
    public VkSwapchainKHR swapChain;
    public VkCompositeAlphaFlagsKHR composeAlpha;


    public EngineImage[] SwapChainImages;
    public VkSemaphore[] ReadyToPresentToSwapchainSemaphores;
    public VkSemaphore[] AcqforblitSemaphores;
    public int presenterState;
    public VkFormat swapChainImageFormat;
    public Action[] CleanupQueue;

    public VkSurfaceFormatKHR surfaceFormat;
    public VkPresentModeKHR presentMode;


    public bool swapchainImagesShared;
    
    public NSWindow macwindow;
    
    public nint HWND, HINSTANCE;
}

