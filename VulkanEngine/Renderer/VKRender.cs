using System.Reflection;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Assimp;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.ImGui;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private const int Width=800;
    private const int Height=600;

    private static readonly bool EnableValidationLayers =
#if DEBUG
        true;
#else
        false;
#endif
            


    private static readonly string[] validationLayers = {
#if DEBUG
        "VK_LAYER_KHRONOS_validation"
#endif
    };
    
    private static readonly string[] deviceExtensions = {
        KhrSwapchain.ExtensionName,
        KhrDrawIndirectCount.ExtensionName,
        "VK_EXT_descriptor_indexing",

#if MAC
            "VK_KHR_portability_subset"
#endif
    };

    private static readonly string[] instanceExtensions = {
    };
    
    
    public static IWindow? window;
    public static Vk vk=null!;

    private static Instance instance;

    private static ExtDebugUtils? debugUtils;
    private static DebugUtilsMessengerEXT debugMessenger;

    private static Queue transferQueue;
    private static Queue computeQueue;
    private static Queue graphicsQueue;
    private static Queue presentQueue;
    
    private static KhrSurface khrSurface = null!;
    private static SurfaceKHR surface;
    private static KhrSwapchain khrSwapChain = null!;
    private static SwapchainKHR swapChain;
    private static Image[] swapChainImages = null!;
    private static ImageView[] swapChainImageViews=null!;
    private static Format swapChainImageFormat;
    private static Extent2D swapChainExtent;
    private static Framebuffer[]? swapChainFramebuffers;
    
    public static RenderPass RenderPass;
    

    private static PhysicalDevice physicalDevice;
    public static Device device;
    private static DescriptorSetLayout DescriptorSetLayout;
    private static PipelineLayout GfxPipelineLayout;
    private static Pipeline GraphicsPipeline;

    private static Pipeline ComputePipeline;
    public static PipelineLayout ComputePipelineLayout;
    private static DescriptorSetLayout ComputeDescriptorSetLayout;
    private static DescriptorSet ComputeDescriptorSet;


    
    const int FRAME_OVERLAP = 3;
    public static int CurrentFrame = 0;
    static int CurrentFrameIndex = 0;
    static bool FramebufferResized = false;
    
    public static FrameData[] FrameData = null!;
    public static  FrameData GetCurrentFrame()=> FrameData[CurrentFrameIndex];
    public static FrameData GetLastFrame()=> FrameData[(CurrentFrameIndex+FRAME_OVERLAP-1)%FRAME_OVERLAP];
    public static Action[] FrameCleanup = null!;

    private static IInputContext Input;
    public static ImGuiController imGuiController = null!;
    public static void InitializeRenderer()
    {
        InitWindow();
        LoadMesh();
        InitVulkan();
        Input=window.CreateInput();
        imGuiController = new ImGuiController(vk,window,Input,new ImGuiFontConfig(AssetsPath+"/fonts/FiraSansCondensed-ExtraLight.otf",12),physicalDevice,_familyIndices.graphicsFamily!.Value,swapChainImages.Length,swapChainImageFormat,GlobalData.depthFormat);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
    }

 
    public static void Render()
    {
        DrawFrame();
        CurrentFrame++;
        CurrentFrameIndex =CurrentFrame% FRAME_OVERLAP;
    }


public static string AssetsPath
{
    get
    {
        if (_RPath != null) return _RPath;
        var f=System.IO.Directory.GetParent(Assembly.GetExecutingAssembly().Location);
        while (f.GetDirectories("Assets").Length==0)
        {
            f=f.Parent!;
            if (f==null) throw new Exception("Assets folder not found");
        }
        _RPath=f.FullName+"/Assets";
        return _RPath;
    }
}


private static string _RPath;

public static void DoWhatMustBeDone()
{
    FrameCleanup[CurrentFrameIndex]();
    FrameCleanup[CurrentFrameIndex]=()=>{};
}

private static unsafe void LoadMesh()
    {
        using var assimp = Assimp.GetApi()!;
        
        var scene=assimp.ImportFile(AssetsPath+"/models/model.obj", (uint)PostProcessPreset.TargetRealTimeMaximumQuality)!;
        
        var vertexMap = new Dictionary<Vertex, uint>();
        var _vertices = new List<Vertex>();
        var _indices = new List<uint>();
        
        VisitSceneNode(scene->MRootNode);
        
        assimp.ReleaseImport(scene);
        
        vertices = _vertices.ToArray();
        indices = _indices.ToArray();
        
        void VisitSceneNode(Node* node)
        {
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];
        
                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];
        
                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices![i];
        
                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords![0]![(int)index];
        
                        Vertex vertex = new()
                        {
                            pos = new Vector3D<float>(position.X, position.Y, position.Z),
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
            }
        
            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]!);
            }
        }
    }



}