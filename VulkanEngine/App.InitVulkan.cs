using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine;

public static partial class App
{
    private static void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        // Analize
        CreateLogicalDevice();
        CreateSwapChain();
        CreateSwapChainImages();
        CreateRenderPass();
        CreateGraphicsPipeline();
        CreateFramebuffers();
        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();
    }

    private static unsafe void CreateSyncObjects()
    {
        var semCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
        };
        var fenceCreateInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };
        imageAvailableSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        renderFinishedSemaphores = new Semaphore[MAX_FRAMES_IN_FLIGHT];
        inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];
        
        for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
        {
            vk!.CreateSemaphore(logicalDevice, semCreateInfo, null, out imageAvailableSemaphores[i])
                .Expect("failed to create semaphore!");
            vk!.CreateSemaphore(logicalDevice, semCreateInfo, null, out renderFinishedSemaphores[i])
                .Expect("failed to create semaphore!");
            vk!.CreateFence(logicalDevice, fenceCreateInfo, null, out inFlightFences[i])
                .Expect("failed to create fence!");
        }
        
    }

    private static unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = 0,
            PInheritanceInfo = null,
        };
        vk!.BeginCommandBuffer(commandBuffer, beginInfo)
            .Expect("failed to begin recording command buffer!");

        var clearValues = new ClearValue
        {
            Color = new ClearColorValue(1,1,1,1)
        };
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = RenderPass,
            Framebuffer = swapChainFramebuffers[imageIndex],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChainExtent,
            },
            ClearValueCount = 1,
            
            PClearValues = &clearValues
            
        };
        var viewPort = new Viewport()
        {
            X = 0,
            Y = 0,
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 0,
        };
        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = swapChainExtent,
        };

        vk!.CmdBeginRenderPass(commandBuffer, renderPassInfo, SubpassContents.Inline);
        vk!.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, GraphicsPipeline);
        vk!.CmdSetViewport(commandBuffer,0,1,&viewPort);
        vk!.CmdSetScissor(commandBuffer,0,1,&scissor);
        vk!.CmdDraw(commandBuffer, 3, 1, 0, 0);
        vk!.CmdEndRenderPass(commandBuffer);
        vk!.EndCommandBuffer(commandBuffer)
            .Expect("failed to record command buffer!");
    }
    private static void CreateCommandBuffers()
    {
        CommandBuffers = new CommandBuffer[MAX_FRAMES_IN_FLIGHT];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = CommandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1,
        };
        for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            vk!.AllocateCommandBuffers(logicalDevice, allocInfo,out CommandBuffers[i])
                .Expect("failed to allocate command buffers!");
    }

    private static unsafe void CreateCommandPool()
    {
        var queueFamilyIndices = FindQueueFamilies(physicalDevice);
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.graphicsFamily!.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
        };
        vk!.CreateCommandPool(logicalDevice, poolInfo, null, out CommandPool)
            .Expect("failed to create command pool!");
    }

    private static unsafe void CreateFramebuffers()
    {
        var imageCount = swapChainImageViews!.Length;
        swapChainFramebuffers = new Framebuffer[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            var attachments = stackalloc[] {swapChainImageViews[i]};
            var framebufferCreateInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = RenderPass,
                AttachmentCount = 1,
                PAttachments = attachments,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                Layers = 1,
            };
            vk!.CreateFramebuffer(logicalDevice, framebufferCreateInfo, null, out swapChainFramebuffers[i])
                .Expect("failed to create framebuffer!");
        }
    }

    private static unsafe void CreateRenderPass()
    {
        var subpassDependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            SrcAccessMask = 0,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
            DstAccessMask =
                // AccessFlags.ColorAttachmentReadBit |
                AccessFlags.ColorAttachmentWriteBit,
        };
        
        var colorAttachment = new AttachmentDescription
        {
Format = swapChainImageFormat,
Samples = SampleCountFlags.Count1Bit,
LoadOp = AttachmentLoadOp.Clear,
StoreOp = AttachmentStoreOp.Store,
StencilLoadOp = AttachmentLoadOp.DontCare,
StencilStoreOp = AttachmentStoreOp.DontCare,
InitialLayout = ImageLayout.Undefined,
FinalLayout = ImageLayout.PresentSrcKhr,
        };
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
        };
        var renderPassCreateInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 1,
            PAttachments = &colorAttachment,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &subpassDependency,
        };
        vk!.CreateRenderPass(logicalDevice, renderPassCreateInfo, null, out RenderPass)
            .Expect("failed to create render pass!");
    }

    private static unsafe void CreateGraphicsPipeline()
    {
        byte[] vertexShaderCode = File.ReadAllBytes("/Users/yavuz/Git/VulkanEngine/VulkanEngine/vert.spv");
        byte[] fragmentShaderCode = File.ReadAllBytes("/Users/yavuz/Git/VulkanEngine/VulkanEngine/frag.spv");
        
        var vertexModule = CreateShaderModule(vertexShaderCode);
        var fragmentModule = CreateShaderModule(fragmentShaderCode);
        
        var MarshaledString = SilkMarshal.StringToPtr("main");
        var vePSSCI = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertexModule,
            PName = (byte*) MarshaledString,
        };
        var pxPSSCI = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragmentModule,
            PName = (byte*) MarshaledString
        };
        var combinedStages = stackalloc[] {pxPSSCI, vePSSCI};
        
        
        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
SType = StructureType.PipelineVertexInputStateCreateInfo,
VertexBindingDescriptionCount = 0,
PVertexBindingDescriptions = null,
VertexAttributeDescriptionCount = 0,
PVertexAttributeDescriptions = null,
        };

        var inputAss = new PipelineInputAssemblyStateCreateInfo
        {
SType = StructureType.PipelineInputAssemblyStateCreateInfo,
Topology = PrimitiveTopology.TriangleList,
PrimitiveRestartEnable = false,
        };
        
        
        
        
        var dynamicStates = stackalloc[] { DynamicState.Viewport,DynamicState.Scissor};

        var dynamicStateCreateInfo = new PipelineDynamicStateCreateInfo
        {
            SType = StructureType.PipelineDynamicStateCreateInfo,
            DynamicStateCount = 2,
            PDynamicStates = dynamicStates
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            ScissorCount = 1,
        };

        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1,
            CullMode = CullModeFlags.BackBit,
            FrontFace = FrontFace.Clockwise,
            DepthBiasEnable = false,
            DepthBiasConstantFactor = 0,
            DepthBiasClamp = 0,
            DepthBiasSlopeFactor = 0,
        };

        var multiSample = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit,
            MinSampleShading = 1,
            PSampleMask = null,
            AlphaToCoverageEnable = false,
            AlphaToOneEnable = false,
        };
        
        
        PipelineColorBlendAttachmentState colorBlendAttachment = new()
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false,
        };

        PipelineColorBlendStateCreateInfo colorBlending = new()
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            LogicOp = LogicOp.Copy,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment,
            
        };

        colorBlending.BlendConstants[0] = 0;
        colorBlending.BlendConstants[1] = 0;
        colorBlending.BlendConstants[2] = 0;
        colorBlending.BlendConstants[3] = 0;

        PipelineLayoutCreateInfo pipelineLayoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 0,
            PushConstantRangeCount = 0,
        };

        vk!.CreatePipelineLayout(logicalDevice,  pipelineLayoutInfo, null, out PipelineLayout)
            .Expect("failed to create pipeline layout!");
        var GFXpipeline = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = combinedStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAss,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multiSample,
            PDepthStencilState = null,
            PColorBlendState = &colorBlending,
            PDynamicState = &dynamicStateCreateInfo,
            Layout = PipelineLayout,
            RenderPass = RenderPass,
            Subpass = 0,
            BasePipelineHandle = default,
        };
        vk.CreateGraphicsPipelines(logicalDevice, default, 1, &GFXpipeline, null, out GraphicsPipeline)
            .Expect("failed to create graphics pipeline!");
        
        vk!.DestroyShaderModule(logicalDevice,vertexModule,null);
        vk!.DestroyShaderModule(logicalDevice,fragmentModule,null);
    }

    static unsafe ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var shaderCreateInfo = new ShaderModuleCreateInfo()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) code.Length,
                PCode = (uint*) pCode,
            };
            vk!.CreateShaderModule(logicalDevice, shaderCreateInfo, null, out var result)
                .Expect("failed to create shader module!");
            return result;
        }

    }
    private static unsafe void CreateSwapChainImages()
    {
        var imageCount = swapChainImages!.Length;
        swapChainImageViews = new ImageView[imageCount];
        for (int i = 0; i < imageCount; i++)
        {
            var viewCreateInfo = new ImageViewCreateInfo()
            {
              SType  = StructureType.ImageViewCreateInfo,
              Image = swapChainImages[i],
              ViewType = ImageViewType.Type2D,
              Format = swapChainImageFormat,
              Components = new ComponentMapping()
              {
                  R = ComponentSwizzle.Identity,
                  G = ComponentSwizzle.Identity,
                  B = ComponentSwizzle.Identity,
                  A = ComponentSwizzle.Identity,
              },
              SubresourceRange = new ImageSubresourceRange()
              {
                  AspectMask = ImageAspectFlags.ColorBit,
                  BaseMipLevel = 0,
                  LevelCount = 1,
                  BaseArrayLayer = 0,
                  LayerCount = 1,
              },
            };
            vk!.CreateImageView(logicalDevice,viewCreateInfo, null, out swapChainImageViews[i])
                .Expect("failed to create image views!");
        }
    }

    private static unsafe void CreateSurface()
    {
        if (!vk!.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    }
    

    private static PhysicalDevice physicalDevice;
    private static Device logicalDevice;
    private static PipelineLayout PipelineLayout;
    private static Pipeline GraphicsPipeline;

    private static unsafe void PickPhysicalDevice()
    {
        
        uint deviceCount = 0;
        vk!.EnumeratePhysicalDevices(instance, &deviceCount, null);
        if (deviceCount==0)
        {
            throw new("failed to find GPUs with Vulkan support!");
        }

        var list = new PhysicalDevice[deviceCount];
        
        fixed(PhysicalDevice* n_list=list)
            vk!.EnumeratePhysicalDevices(instance, &deviceCount, n_list);

        foreach (var device in list)
        {
            if (IsDeviceSuitable(device))
            {
                physicalDevice = device;
                break;
            }
        }
        if (physicalDevice.Handle==0)
        {
            throw new ("failed to find a suitable GPU!");
        }
        
        
    }
    struct QueueFamilyIndices
    {
        public uint? graphicsFamily { get; set; }
        public uint? presentFamily { get; set; }
        
        public bool IsComplete()
        {
            return graphicsFamily.HasValue&&
                   presentFamily.HasValue&&
                   true;
        }
    }

    private static unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilityCount];
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk!.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamiliesPtr);
        }


        uint i = 0;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.graphicsFamily = i;
            }
            Bool32 presentSupport = false;
            khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, &presentSupport);
            if (presentSupport)
            {
                indices.presentFamily = i;
            }
            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    private static bool IsDeviceSuitable(PhysicalDevice device)
    {
        
        var indices = FindQueueFamilies(device);

        var extensionsSupported = CheckDeviceExtensionSupport(device);
        return indices.IsComplete()&& extensionsSupported;

        
        
        var properties = vk.GetPhysicalDeviceProperties(device);
        var features = vk.GetPhysicalDeviceFeatures(device);

        return
            
            // properties.DeviceType==PhysicalDeviceType.DiscreteGpu&&
            // features.GeometryShader&&
            true
            ;
    }

    private static unsafe bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint extensionCount = 0;
        vk.EnumerateDeviceExtensionProperties(device, (byte*)null, &extensionCount, null);
        var avaliableExtension = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* avaliableExtensionPtr = avaliableExtension)
            vk.EnumerateDeviceExtensionProperties(device, (byte*) null, ref extensionCount, avaliableExtensionPtr);

        var availableExtensionNames = avaliableExtension.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();
        return deviceExtensions.All(availableExtensionNames.Contains);    
    }

    private static unsafe void CreateInstance()
    {
        vk = Vk.GetApi();

        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("validation layers requested, but not available!");
        }

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version11
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
#if true
        extensions = extensions.Append("VK_KHR_portability_enumeration").ToArray();
#endif

        
        
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
#if true//mac
        createInfo.Flags |= InstanceCreateFlags.EnumeratePortabilityBitKhr;
#endif

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk!.CreateInstance(createInfo, null, out instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private static unsafe void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivalent to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!vk!.TryGetInstanceExtension(instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }
    
    private static unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {

        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback;
    }

    private static unsafe string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
        //if mac
        extensions = extensions!.Append("VK_KHR_portability_enumeration").ToArray();
        //endif
        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    private static unsafe bool CheckValidationLayerSupport()
    {
        uint layerCount = 0;
        vk!.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }

    private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        Console.WriteLine($"validation layer:" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

        return Vk.False;
    }
}