using System.Runtime.CompilerServices;
using ImGuiNET;
using Pastel;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Image = Silk.NET.Vulkan.Image;

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
    
    public static (Pipeline pipeline, PipelineLayout pipelineLayout) CreatePSO(
        ReadOnlySpan<PipelineShaderStageCreateInfo> shaderStages,
        VertexInputAttributeDescription[] VertexDefinition,
        ReadOnlySpan<DynamicState> dynamicStates,
        PipelineRasterizationStateCreateInfo rasterizer,
        PipelineMultisampleStateCreateInfo multisampling,
        PipelineDepthStencilStateCreateInfo depthStencil,
        PipelineColorBlendAttachmentState colorBlendAttachment,
        PipelineColorBlendStateCreateInfo colorBlending,
        ReadOnlySpan<DescriptorSetLayout> descriptorSetLayouts
        )
    {
        unsafe
        {
            fixed(PipelineShaderStageCreateInfo* shaderStagesPtr = shaderStages)
            fixed(DynamicState* dynamicStatesPtr = dynamicStates)
            fixed(DescriptorSetLayout* descriptorSetLayoutsPtr= descriptorSetLayouts)
            {
                
                throw new NotImplementedException();
            }    
        }
    }


    public static unsafe (Pipeline pipeline, PipelineLayout pipelineLayout) CreateComputePSO(
        PipelineShaderStageCreateInfo shaderStage,
        Span<DescriptorSetLayout> descriptorSetLayouts
    )
    {
        fixed(DescriptorSetLayout* descriptorSetLayoutsPtr= descriptorSetLayouts)
        {
            var computePipelineLayoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = (uint) descriptorSetLayouts.Length,
                PSetLayouts =  (descriptorSetLayoutsPtr),

            };
            vk.CreatePipelineLayout(device, &computePipelineLayoutInfo, null, out var layout);
            var computePipelineInfo = new ComputePipelineCreateInfo
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Stage = shaderStage,
                Layout = layout,
            };
            vk.CreateComputePipelines(device, default, 1, &computePipelineInfo, null, out var pipeline)
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
        raw.window = Window.Create(options);
        raw.window.Initialize();
        var type = raw.window.GetType();
        Console.WriteLine($"window named {options.Title} initialized with as {type} ");
        
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if(device.Handle==default) InitVulkanFirstPhase(raw.window!.VkSurface!.GetRequiredExtensions(out var extC),(int)extC);

            // Create Vulkan Surface
            if (!vk!.TryGetInstanceExtension(instance, out khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }
    
            unsafe
            {
                raw.surface = raw.window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
            }
            if(device.Handle==default) InitVulkanSecondPhase(raw.surface);

        }
        
        
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

            window.swapChainImageFormat = window.surfaceFormat.Format;


            var imageCount = deviceSwapChainSupport.Capabilities.MinImageCount + 1;
            if (deviceSwapChainSupport.Capabilities.MaxImageCount > 0 &&
                imageCount > deviceSwapChainSupport.Capabilities.MaxImageCount)
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
                ImageExtent = window.size,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            };

            var queueFamilyIndices = stackalloc[] {DeviceInfo.indices.graphicsFamily!.Value, DeviceInfo.indices.presentFamily!.Value};

             window.swapchainImagesShared = DeviceInfo.indices.graphicsFamily != DeviceInfo.indices.presentFamily;
            if (window.swapchainImagesShared)
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
                    throw new NotSupportedException(
                        "CompositeAlphaFlagsKHR.PostMultipliedBitKhr or CompositeAlphaFlagsKHR.PreMultipliedBitKhr not supported. yet transparency is requested");
                }

                compositeMode = alphaSupport.HasFlag(CompositeAlphaFlagsKHR.PreMultipliedBitKhr)
                    ? CompositeAlphaFlagsKHR.PreMultipliedBitKhr
                    : CompositeAlphaFlagsKHR.PostMultipliedBitKhr;
            }
            else
            {
                compositeMode = CompositeAlphaFlagsKHR.OpaqueBitKhr;
            }


            window.composeAlpha = compositeMode;
            creatInfo = creatInfo with
            {
                PreTransform = deviceSwapChainSupport.Capabilities.CurrentTransform,
                // opaque if not needed, premultiplied if supported, else postmultiplied 
                CompositeAlpha = compositeMode,
                PresentMode = window.presentMode,
                Clipped = true,


                OldSwapchain = default //todo pass in old swapchain
            };

            Console.WriteLine(
                $"created swapchain with compositeMode: {compositeMode} and presentMode: {window.presentMode}".Pastel(
                    ConsoleColor.Green));

            if (!vk.TryGetDeviceExtension(instance, device, out khrSwapChain!))
            {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }

            khrSwapChain.CreateSwapchain(device, creatInfo, null, out window.swapChain)
                .Expect("failed to create swap chain!");


            khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, null).Expect();

            window.SwapChainImages = new ScreenSizedImage[imageCount];
            
            var tmp_swapchain = stackalloc Silk.NET.Vulkan.Image[(int)imageCount];

            khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, tmp_swapchain);
            
            
            for (int i = 0; i < imageCount; i++)
            {
                var imageview = CreateImageView(tmp_swapchain[i], window.swapChainImageFormat, ImageAspectFlags.ColorBit);
                
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
            window.depthImage = AllocateScreenSizedImage(window.size.ToInt2(),FindDepthFormat(),ImageUsageFlags.DepthStencilAttachmentBit,MemoryPropertyFlags.DeviceLocalBit,ImageAspectFlags.DepthBit ,false);
            TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);

        }
    }

    public static unsafe void ResizeSwapChain(EngineWindow window, int2 newsize)
    {
        // we should not be touching in use items
        // vk.DeviceWaitIdle(device);
        
        // queue swap chain image/view/depth/framebuff cleanup
        window.resizeFrameNo = CurrentFrame;

        var a = new SwapchainCreateInfoKHR()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = window.surface,

            MinImageCount = (uint) window.SwapChainImages.Length,
            ImageFormat = window.surfaceFormat.Format,
            ImageColorSpace = window.surfaceFormat.ColorSpace,
            ImageExtent = new((uint)newsize.X, (uint)newsize.Y),
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            
            
            PreTransform = deviceSwapChainSupport.Capabilities.CurrentTransform,
            // opaque if not needed, premultiplied if supported, else postmultiplied 
            CompositeAlpha = window.composeAlpha,
            PresentMode = window.presentMode,
            Clipped = true,


            OldSwapchain = window.swapChain 
        };
        
        var queueFamilyIndices = stackalloc[] {DeviceInfo.indices.graphicsFamily!.Value, DeviceInfo.indices.presentFamily!.Value};

        if (window.swapchainImagesShared)
        {
            a = a with
            {
                ImageSharingMode = SharingMode.Concurrent,
                QueueFamilyIndexCount = 2,
                PQueueFamilyIndices = queueFamilyIndices,
            };
        }
        else
        {
            a.ImageSharingMode = SharingMode.Exclusive;
        }
        khrSwapChain.CreateSwapchain(device, &a,null,out var new_swapChain)
            .Expect();
        var imagecount = window.SwapChainImages.Length;
        var tmp_swapchain = stackalloc Silk.NET.Vulkan.Image[(int)imagecount];
        khrSwapChain.GetSwapchainImages(device,new_swapChain,(uint*)&imagecount,tmp_swapchain)
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
                CreateImageView(tmp_swapchain[i], window.swapChainImageFormat, ImageAspectFlags.ColorBit),
                CurrentFrame);
        }
        window.depthImage.EnqueueDestroy();
        window.depthImage = AllocateScreenSizedImage(newsize,window.depthImage.ImageFormat,window.depthImage.ImageUsage,window.depthImage.MemoryProperties, ImageAspectFlags.DepthBit, false );
        TransitionImageLayout(window.depthImage.Image, window.depthImage.ImageFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);

        khrSwapChain.DestroySwapchain(device,window.swapChain,null);
        window.swapChain = new_swapChain;
        window.size = new((uint)newsize.X, (uint)newsize.Y);
        return;
       
        
    }
    
    public static ScreenSizedImage AllocateScreenSizedImage(int2 size, Format format, ImageUsageFlags usage,
        MemoryPropertyFlags props, ImageAspectFlags aspect, bool preserveOnResize)
    {
        unsafe
        {
            Image image;
            DeviceMemory mem;
            CreateImage((uint) size.X, (uint) size.Y,
                format,
                ImageTiling.Optimal,
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
    public SurfaceKHR surface;
    public Extent2D size;
    public int resizeFrameNo;
    // public string title;
    // public bool vsync;
    public bool transparency;
    // public WindowBorder windowBorder;
    // public int2 position;
    // public KhrSwapchain khrSwapChain;
    public SwapchainKHR swapChain;
    public CompositeAlphaFlagsKHR composeAlpha;


    public ScreenSizedImage[] SwapChainImages;
    
    public Format swapChainImageFormat;

    public SurfaceFormatKHR surfaceFormat;
    public PresentModeKHR presentMode;

    // window depth image
    public ScreenSizedImage depthImage;

    public ScreenSizedImage[] extraImages;
    public bool swapchainImagesShared;
}

public struct ScreenSizedImage
{
    public int2 size; 
    public Image Image;
    public MemoryPropertyFlags MemoryProperties;
    public ImageUsageFlags ImageUsage;
    public Format ImageFormat;
    public DeviceMemory DeviceMemory;
    public ImageView ImageView;
    public int creationFrame;
    
    public bool PreserveOnResize;
    
    public ScreenSizedImage(int2 size, Format imageFormat, bool preserveOnResize, Image image,
        ImageUsageFlags usage,MemoryPropertyFlags memoryProperties, DeviceMemory deviceMemory,
        ImageView imageView,int CreationFrame)
    {
        this.size = size;
        creationFrame = CreationFrame;
        Image = image;
        ImageUsage = usage;
        this.MemoryProperties = memoryProperties;
        ImageFormat = imageFormat;
        DeviceMemory = deviceMemory;
        ImageView = imageView;
        PreserveOnResize = preserveOnResize;
    }


    
    public void DestroyImmediate()
    {
        unsafe
        {
            if(DeviceMemory.Handle!=default) VKRender.vk.DestroyImage(VKRender.device, Image, null);
            if(true) VKRender.vk.DestroyImageView(VKRender.device, ImageView, null);
            if(DeviceMemory.Handle!=default) VKRender.vk.FreeMemory(VKRender.device, DeviceMemory, null);
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

