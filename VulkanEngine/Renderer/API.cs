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
    public static void SetCamera(Transform_ref transform, VulkanEngine.CameraData camera)
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
                (float) swapChainExtent.Width / swapChainExtent.Height, camera.nearPlaneDistance, camera.farPlaneDistance),
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
    
    public static EngineWindow CreateWindowRaw(
        WindowOptions options
    )
    {
        var raw = new EngineWindow();
        raw.window = Window.Create(options);
        raw.window.Initialize();
        var type = raw.window.GetType();
        Console.WriteLine($"window named {options.Title} initialized with as {type} ");
        
        {
            // Create Vulkan Surface
            if (!vk.TryGetInstanceExtension(instance, out raw.khrSurface))
            {
                throw new NotSupportedException("KHR_surface extension not found.");
            }

            unsafe
            {
                raw.surface = raw.window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
            }
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


            deviceSwapChainSupport = QuerySwapChainSupport(physicalDevice);
            window.surfaceFormat = ChooseSwapSurfaceFormat(deviceSwapChainSupport.Formats);
            window.presentMode = ChoosePresentMode(deviceSwapChainSupport.PresentModes);
            window.size = ChooseSwapExtent(deviceSwapChainSupport.Capabilities);



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

            var indices = DeviceInfo.indices;
            var queueFamilyIndices = stackalloc[] {indices.graphicsFamily!.Value, indices.presentFamily!.Value};

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

            if (!vk.TryGetDeviceExtension(instance, device, out window.khrSwapChain))
            {
                throw new NotSupportedException("VK_KHR_swapchain extension not found.");
            }

            window.khrSwapChain.CreateSwapchain(device, creatInfo, null, out window.swapChain)
                .Expect("failed to create swap chain!");


            khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, null).Expect();

            window.swapChainImages = new Silk.NET.Vulkan.Image[imageCount];
            fixed (Silk.NET.Vulkan.Image* swapChainImagesPtr = window.swapChainImages)
            {
                khrSwapChain.GetSwapchainImages(device, window.swapChain, ref imageCount, swapChainImagesPtr);
            }

            window.swapChainImageFormat = window.surfaceFormat.Format;


        }
    }

    public static void ResizeSwapChain(EngineWindow window, int2 size)
    {
        // we should not be touching in use items
        // vk.DeviceWaitIdle(device);
        
        // queue swap chain image/view/depth/framebuff cleanup
        CleanUpSwapChainStuff(window);

        CreateSwapChain(window);
        CreateSwapChainImageViews(window);
            
        // CreateRenderPass();
        CreateDepthResources();
        
        // CreateGraphicsPipeline();
        CreateSwapchainFrameBuffers();
    }


    public static ScreenSizedImage CreateScreenSizedImage(
    )
    {
        
        
        
        return new()
        {
            
        }
    }
}

public class EngineWindow
{
    public IWindow window;
    public nint Handle;
    public KhrSurface khrSurface;
    public SurfaceKHR surface;
    public Extent2D size;
    public string title;
    public bool vsync;
    public bool transparency;
    public WindowBorder windowBorder;
    public int2 position;
    public KhrSwapchain khrSwapChain;
    public SwapchainKHR swapChain;
    public CompositeAlphaFlagsKHR composeAlpha;


    public ScreenSizedImage[] SwapChainImages;
    
    public Format swapChainImageFormat;

    public SurfaceFormatKHR surfaceFormat;
    public PresentModeKHR presentMode;

    // window depth image
    public ScreenSizedImage depthImage;

    public ScreenSizedImage[] extraImages;
}

public struct ScreenSizedImage
{
    public int2 size; 
    public Image Image;
    public Format ImageFormat;
    public DeviceMemory DeviceMemory;
    public ImageView ImageView;

    public bool PreserveOnResize;
    
    public ScreenSizedImage(int2 size, Format imageFormat,  bool preserveOnResize)
    {
        this.size = size;
        Image = image;
        ImageFormat = imageFormat;
        DeviceMemory = deviceMemory;
        ImageView = imageView;
        PreserveOnResize = preserveOnResize;
    }
    
    public void DestroyImmediate()
    {
        unsafe
        {
            VKRender.vk.DestroyImage(VKRender.device, Image, null);
            VKRender.vk.DestroyImageView(VKRender.device, ImageView, null);
            VKRender.vk.FreeMemory(VKRender.device, DeviceMemory, null);
        }
    }

    public void EnqueueDestroy()
    {
        VKRender.FrameCleanup[VKRender.CurrentFrameIndex]+=(DestroyImmediate);
    }
}

