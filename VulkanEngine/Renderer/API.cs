using System.Runtime.CompilerServices;
using ImGuiNET;
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
    
}

public class EngineWindow
{
    public IWindow window;
    public nint Handle;
    public KhrSurface khrSurface;
    public SurfaceKHR surface;
    public int2 size;
    public string title;
    public bool vsync;
    public bool transparency;
    public WindowBorder windowBorder;
    public int2 position;
    public KhrSwapchain khrSwapChain;
    public SwapchainKHR swapChain;
    public Image[] swapChainImages;
    public ImageView[] swapChainImageViews;
    public Format swapChainImageFormat;
    public Extent2D swapChainExtent;
    public Framebuffer[] swapChainFramebuffers;
    
}
