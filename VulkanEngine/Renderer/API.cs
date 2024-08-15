// using ImGuiNET;

using System.Diagnostics.Contracts;
using System.Numerics;
using Cathei.LinqGen;
using OSBindingTMP;
using Pastel;
using Silk.NET.Maths;
using Vortice.Vulkan;
using WindowsBindings;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public partial class VKRender
{
    // public Mesh LoadMeshAssimp(string path)
    // {
    //     return new Mesh();
    // }
    public static void SetCamera(Transform_ref transform, VulkanEngine.CameraData camera, int2 windowSize)
    {
        // Console.WriteLine($"{transform.world_position} {transform.forward} {transform.up} {camera.fov} {camera.nearPlaneDistance} {camera.farPlaneDistance}");
        // ImGui.Begin("SetCamera");
        
        currentCamera = new Camera
        {
            view = Matrix4X4.CreateLookAt(
                transform.world_position, 
                transform.world_position + transform.forward, 
                // new float3(0,0,0),
                transform.up),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(camera.fov),
                ((float) windowSize.X) / windowSize.Y, camera.nearPlaneDistance, camera.farPlaneDistance),
        };
        currentCamera.proj.M22 *= -1;
        
        // ImGui.Text($"Camera view:\n {currentCamera.view.Row1:F3}\n{currentCamera.view.Row2:F3}\n{currentCamera.view.Row3:F3}\n{currentCamera.view.Row4:F3}");
        Matrix4X4.Decompose(currentCamera.view, out var scale, out var rotation, out var translation);
        // ImGui.Text($"Decomposed view:\n {scale:F3} \n {Vector3D.Transform(float3.One,rotation)*180f/float.Pi:F3}\n {translation:F3}");
        // ImGui.End();
    }

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

    public static EngineWindow CreateWindow(
        int2 size,
        string title,
        bool vsync = true,
        bool transparency = false,
        object? windowBorder = null,
        int2? position = null
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
    
    public static unsafe EngineWindow CreateWindowRaw(
    )
    {
        var raw = new EngineWindow();
        switch (MIT.OS)
        {
            case OSType.Mac:
            {
                var app = OSBindingTMP.MacBinding.create_application();
                var window = MacBinding.open_window("Test",
                    800,
                    600,
                    0,
                    0,
                    MacBinding.NSWindowStyleMask.NSWindowStyleMaskTitled
                    | MacBinding.NSWindowStyleMask.NSWindowStyleMaskMiniaturizable
                    | MacBinding.NSWindowStyleMask.NSWindowStyleMaskResizable
                    //|MacBinding.NSWindowStyleMask.NSWindowStyleMaskClosable
                );
                var surface_ptr = OSBindingTMP.MacBinding.window_create_surface(window);
                MacBinding.window_makeKeyAndOrderFront(window);
                raw.macwindow = window;
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                if (instance == null) InitVulkanFirstPhase();

                var macOsSurfaceCreateInfoMvk = new VkMetalSurfaceCreateInfoEXT()
                {
                    flags = VkMetalSurfaceCreateFlagsEXT.None,
                    pLayer = surface_ptr,
                };
                vkCreateMetalSurfaceEXT(instance, &macOsSurfaceCreateInfoMvk, null, out var surface).Expect();

                raw.surface = surface;
            }
                break;
            case OSType.Windows:
            {
                InitVulkanFirstPhase();

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

        
    

        if(device.Handle==default) InitVulkanSecondPhase(raw.surface);

        
        
        
        // if (raw.window.VkSurface is null)
        // {
        //     throw new Exception("Windowing platform doesn't support Vulkan.");
        // }
        
        return raw;
    }

    public static void CreateSwapchain(
        EngineWindow window,
        bool preferMailbox
    )
    {
        unsafe
        {
            
            deviceSwapChainSupport = QuerySwapChainSupport(physicalDevice,window.surface);
            window.surfaceFormat = ChooseSwapSurfaceFormat(deviceSwapChainSupport.Formats); //this can dynamicly change
            window.presentMode = ChoosePresentMode(deviceSwapChainSupport.PresentModes);
            window.size = ChooseSwapExtent(window);

            window.swapChainImageFormat = window.surfaceFormat.format;


            var imageCount = deviceSwapChainSupport.Capabilities.minImageCount + 1;
            if (deviceSwapChainSupport.Capabilities.maxImageCount > 0 &&
                imageCount > deviceSwapChainSupport.Capabilities.maxImageCount)
            {
                imageCount = deviceSwapChainSupport.Capabilities.maxImageCount;
            }

            var creatInfo = new VkSwapchainCreateInfoKHR
            {
                surface = window.surface,
                minImageCount = imageCount,
                imageFormat = window.surfaceFormat.format,
                imageColorSpace = window.surfaceFormat.colorSpace,
                imageExtent = window.size,
                imageArrayLayers = 1,
                imageUsage = VkImageUsageFlags.ColorAttachment,
            };

            var queueFamilyIndices = stackalloc[] {DeviceInfo.indices.graphicsFamily!.Value, DeviceInfo.indices.presentFamily!.Value};

             window.swapchainImagesShared = DeviceInfo.indices.graphicsFamily != DeviceInfo.indices.presentFamily;
            if (window.swapchainImagesShared)
            {
                creatInfo = creatInfo with
                {
                    imageSharingMode = VkSharingMode.Concurrent,
                    queueFamilyIndexCount = 2,
                    pQueueFamilyIndices = queueFamilyIndices,
                };
            }
            else
            {
                creatInfo.imageSharingMode = VkSharingMode.Exclusive;
            }

            VkCompositeAlphaFlagsKHR compositeMode;
            if (window.transparency)
            {
                var alphaSupport = deviceSwapChainSupport.Capabilities.supportedCompositeAlpha;
                if ((alphaSupport & (VkCompositeAlphaFlagsKHR.PostMultiplied |
                                     VkCompositeAlphaFlagsKHR.PreMultiplied)) == 0)
                {
                    throw new NotSupportedException(
                        "CompositeAlphaFlagsKHR.PostMultipliedBitKhr or CompositeAlphaFlagsKHR.PreMultipliedBitKhr not supported. yet transparency is requested");
                }

                compositeMode = alphaSupport.HasFlag(VkCompositeAlphaFlagsKHR.PreMultiplied)
                    ? VkCompositeAlphaFlagsKHR.PreMultiplied
                    : VkCompositeAlphaFlagsKHR.PostMultiplied;
            }
            else
            {
                compositeMode = VkCompositeAlphaFlagsKHR.Opaque;
            }


            window.composeAlpha = compositeMode;
            creatInfo = creatInfo with
            {
                preTransform = deviceSwapChainSupport.Capabilities.currentTransform,
                // opaque if not needed, premultiplied if supported, else postmultiplied 
                compositeAlpha = compositeMode,
                presentMode = window.presentMode,
                clipped = true,


                oldSwapchain = default //todo pass in old swapchain
            };

            Console.WriteLine(
                $"created swapchain with compositeMode: {compositeMode} and presentMode: {window.presentMode}".Pastel(
                    ConsoleColor.Green));



            vkCreateSwapchainKHR(device, &creatInfo, null, out window.swapChain)
                .Expect("failed to create swap chain!");


            vkGetSwapchainImagesKHR(device, window.swapChain, & imageCount, null).Expect();

            window.SwapChainImages = new ScreenSizedImage[imageCount];
            
            var tmp_swapchain = stackalloc VkImage[(int)imageCount];

            vkGetSwapchainImagesKHR(device, window.swapChain, & imageCount, tmp_swapchain);
            
            
            for (var i = 0; i < imageCount; i++)
            {
                var imageview = CreateImageView(tmp_swapchain[i], window.swapChainImageFormat, VkImageAspectFlags.Color);
                
                window.SwapChainImages[i] = new ScreenSizedImage(
                    window.size.ToInt2(),
                    window.swapChainImageFormat,
                    false,
                    tmp_swapchain[i], 
                    default,
                    default,
                    default,
                    imageview,
                    CurrentFrame);
            }
            window.depthImage = AllocateScreenSizedImage(window.size.ToInt2(),FindDepthFormat(),VkImageUsageFlags.DepthStencilAttachment,VkMemoryPropertyFlags.DeviceLocal,VkImageAspectFlags.Depth ,false);
            TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal,0,1);

        }
    }

    public static unsafe void ResizeSwapChain(EngineWindow window, int2 newsize)
    {
        // we should not be touching in use items
        // vkDeviceWaitIdle(device);
        
        // queue swap chain image/view/depth/framebuff cleanup
        window.resizeFrameNo = CurrentFrame;

        var a = new VkSwapchainCreateInfoKHR()
       {
            surface = window.surface,
            minImageCount = (uint) window.SwapChainImages.Length,
            imageFormat = window.surfaceFormat.format,
            imageColorSpace = window.surfaceFormat.colorSpace,
            imageExtent = new VkExtent2D((uint)newsize.X, (uint)newsize.Y),
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            
            
            preTransform = deviceSwapChainSupport.Capabilities.currentTransform,
            // opaque if not needed, premultiplied if supported, else postmultiplied 
            compositeAlpha = window.composeAlpha,
            presentMode = window.presentMode,
            clipped = true,


            oldSwapchain = window.swapChain 
        };
        
        var queueFamilyIndices = stackalloc[] {DeviceInfo.indices.graphicsFamily!.Value, DeviceInfo.indices.presentFamily!.Value};

        if (window.swapchainImagesShared)
        {
            a = a with
            {
                imageSharingMode = VkSharingMode.Concurrent,
                queueFamilyIndexCount = 2,
                pQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            a.imageSharingMode = VkSharingMode.Exclusive;
        }
        vkCreateSwapchainKHR(device, &a,null,out var new_swapChain)
            .Expect();
        var imagecount = window.SwapChainImages.Length;
        var tmp_swapchain = stackalloc VkImage[(int)imagecount];
        vkGetSwapchainImagesKHR(device,new_swapChain,(uint*)&imagecount,tmp_swapchain)
            .Expect();

        for (var i = 0; i < imagecount; i++)
        {
            window.SwapChainImages[i].EnqueueDestroy();
            window.SwapChainImages[i] = new ScreenSizedImage(
                newsize,
                window.swapChainImageFormat,
                false,
                tmp_swapchain[i], 
                default,
                default,
                default,
                CreateImageView(tmp_swapchain[i], window.swapChainImageFormat, VkImageAspectFlags.Color),
                CurrentFrame);
        }
        window.depthImage.EnqueueDestroy();
        window.depthImage = AllocateScreenSizedImage(newsize,window.depthImage.ImageFormat,window.depthImage.ImageUsage,window.depthImage.MemoryProperties, VkImageAspectFlags.Depth, false );
        TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal,0,1);

        vkDestroySwapchainKHR(device,window.swapChain,null);
        window.swapChain = new_swapChain;
        window.size = new VkExtent2D((uint)newsize.X, (uint)newsize.Y);
        return;
       
        
    }
    
    public static ScreenSizedImage AllocateScreenSizedImage(int2 size, VkFormat format, VkImageUsageFlags usage,
        VkMemoryPropertyFlags props, VkImageAspectFlags aspect, bool preserveOnResize)
    {
        CreateImage((uint) size.X, (uint) size.Y,
            format,
            VkImageTiling.Optimal,
            usage,
            props,
            false,
            VkImageCreateFlags.None,
            out var image,
            out var mem);

        return new ScreenSizedImage(size,format,preserveOnResize,image,usage,props,mem,CreateImageView(image,format,aspect),CurrentFrame);
    }

    [Pure]
    public static uint MipCount(uint width, uint height) => (uint) (BitOperations.Log2(Math.Max(width, height)) + 1);
}

public class EngineWindow
{
    //public IWindow window;
    // public nint Handle;
    public VkSurfaceKHR surface;
    public VkExtent2D size;
    public int resizeFrameNo;
    // public string title;
    // public bool vsync;
    public bool transparency;
    // public WindowBorder windowBorder;
    // public int2 position;
    // public KhrSwapchain khrSwapChain;
    public VkSwapchainKHR swapChain;
    public VkCompositeAlphaFlagsKHR composeAlpha;


    public ScreenSizedImage[] SwapChainImages;
    
    public VkFormat swapChainImageFormat;

    public VkSurfaceFormatKHR surfaceFormat;
    public VkPresentModeKHR presentMode;

    // window depth image
    public ScreenSizedImage depthImage;

    public ScreenSizedImage[] extraImages;
    public bool swapchainImagesShared;
    
    public NSWindow macwindow;
    
    public nint HWND, HINSTANCE;
}

public struct ScreenSizedImage
{
    public int2 size; 
    public VkImage Image;
    public VkMemoryPropertyFlags MemoryProperties;
    public VkImageUsageFlags ImageUsage;
    public VkFormat ImageFormat;
    public VkDeviceMemory VkDeviceMemory;
    public VkImageView ImageView;
    public int creationFrame;
    
    public bool PreserveOnResize;
    
    public ScreenSizedImage(int2 size, VkFormat imageFormat, bool preserveOnResize, VkImage image,
        VkImageUsageFlags usage,VkMemoryPropertyFlags memoryProperties, VkDeviceMemory deviceMemory,
        VkImageView imageView,int CreationFrame)
    {
        this.size = size;
        creationFrame = CreationFrame;
        Image = image;
        ImageUsage = usage;
        this.MemoryProperties = memoryProperties;
        ImageFormat = imageFormat;
        VkDeviceMemory = deviceMemory;
        ImageView = imageView;
        PreserveOnResize = preserveOnResize;
    }


    
    public void DestroyImmediate()
    {
        unsafe
        {
            if(VkDeviceMemory.Handle!=default) vkDestroyImage(VKRender.device, Image, null);
            if(true) vkDestroyImageView(VKRender.device, ImageView, null);
            if(VkDeviceMemory.Handle!=default) vkFreeMemory(VKRender.device, VkDeviceMemory, null);
        }
    }
/// <summary>
/// destroy on the next acquisition of the same frame <br />
/// which is frame# frame_no+FrameOverlap 
/// </summary>
    public void EnqueueDestroy()
    {
        VKRender.FrameCleanup[(VKRender.CurrentFrameIndex + VKRender.FRAME_OVERLAP - 1) % VKRender.FRAME_OVERLAP]+=(DestroyImmediate);
    }
}

