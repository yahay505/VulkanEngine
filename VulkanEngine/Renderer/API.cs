using ImGuiNET;
using OSBindingTMP;
using Pastel;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Vortice.Vulkan;
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
        ImGui.Begin("SetCamera");
        
        currentCamera = new()
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
        ImGui.Text($"Camera view:\n {currentCamera.view.Row1:F3}\n{currentCamera.view.Row2:F3}\n{currentCamera.view.Row3:F3}\n{currentCamera.view.Row4:F3}");
        Matrix4X4.Decompose(currentCamera.view, out var scale, out var rotation, out var translation);
        ImGui.Text($"Decomposed view:\n {scale:F3} \n {Vector3D.Transform(float3.One,rotation)*180f/float.Pi:F3}\n {translation:F3}");
        ImGui.End();
    }
    
    public static (VkPipeline pipeline, VkPipelineLayout pipelineLayout) CreatePSO(
        ReadOnlySpan<VkPipelineShaderStageCreateInfo> shaderStages,
        VkVertexInputAttributeDescription[] VertexDefinition,
        ReadOnlySpan<VkDynamicState> dynamicStates,
        VkPipelineRasterizationStateCreateInfo rasterizer,
        VkPipelineMultisampleStateCreateInfo multisampling,
        VkPipelineDepthStencilStateCreateInfo depthStencil,
        VkPipelineColorBlendAttachmentState colorBlendAttachment,
        VkPipelineColorBlendStateCreateInfo colorBlending,
        ReadOnlySpan<VkDescriptorSetLayout> descriptorSetLayouts
        )
    {
        unsafe
        {
            fixed(VkPipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
            fixed(VkDynamicState* dynamicStatesPtr = dynamicStates)
            fixed(VkDescriptorSetLayout* descriptorSetLayoutsPtr= descriptorSetLayouts)
            {
                
                throw new NotImplementedException();
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
        WindowBorder? windowBorder = null,
        int2? position = null
    )
    {
        var options = WindowOptions.DefaultVulkan with
        {
            Size = size,
            Title = title,
            VSync = vsync,
            TransparentFramebuffer = transparency,
            WindowBorder = windowBorder??WindowOptions.DefaultVulkan.WindowBorder,
            Position = position??WindowOptions.DefaultVulkan.Position,
        };
        
        return CreateWindowRaw(options);
    }
    
    public static unsafe EngineWindow CreateWindowRaw(
        WindowOptions options
    )
    {
        var raw = new EngineWindow();
        // raw.window = Window.Create(options);
        // raw.window.Initialize();
        // var type = raw.window.GetType();
        var app=OSBindingTMP.MacBinding.create_application();
        var window=OSBindingTMP.MacBinding.open_window("test",600,800,0,0,MacBinding.NSWindowStyleMask.NSWindowStyleMaskClosable|MacBinding.NSWindowStyleMask.NSWindowStyleMaskTitled);
        var surface_ptr=OSBindingTMP.MacBinding.window_create_surface(window);
        raw.macwindow = window;
        // Console.WriteLine($"window named {options.Title} initialized with as {type} ");
        
    
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if(device.Handle==default) InitVulkanFirstPhase();
        
        var macOsSurfaceCreateInfoMvk = new VkMacOSSurfaceCreateInfoMVK()
        {
            flags = VkMacOSSurfaceCreateFlagsMVK.None,
            pView = (void*) surface_ptr,
        };
        vkCreateMacOSSurfaceMVK(instance,&macOsSurfaceCreateInfoMvk, null, out var surface).Expect();
        
        raw.surface = surface;
        if(device.Handle==default) InitVulkanSecondPhase(raw.surface);

        
        
        
        if (raw.window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
        
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

            VkSwapchainCreateInfoKHR creatInfo = new()
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
            
            
            for (int i = 0; i < imageCount; i++)
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
            TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal);

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
            imageExtent = new((uint)newsize.X, (uint)newsize.Y),
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

        for (int i = 0; i < imagecount; i++)
        {
            window.SwapChainImages[i].EnqueueDestroy();
            window.SwapChainImages[i] = new(
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
        TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, VkImageLayout.Undefined, VkImageLayout.DepthStencilAttachmentOptimal);

        vkDestroySwapchainKHR(device,window.swapChain,null);
        window.swapChain = new_swapChain;
        window.size = new((uint)newsize.X, (uint)newsize.Y);
        return;
       
        
    }
    
    public static ScreenSizedImage AllocateScreenSizedImage(int2 size, VkFormat format, VkImageUsageFlags usage,
        VkMemoryPropertyFlags props, VkImageAspectFlags aspect, bool preserveOnResize)
    {
        unsafe
        {
            VkImage image;
            VkDeviceMemory mem;
            CreateImage((uint) size.X, (uint) size.Y,
                format,
                VkImageTiling.Optimal,
                usage,
                props,
                &image,
                &mem);

            return new(size,format,preserveOnResize,image,usage,props,mem,CreateImageView(image,format,aspect),CurrentFrame);
        }
    }
    
}

public class EngineWindow
{
    public IWindow window;
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

