using System.Runtime.InteropServices;
using Silk.NET.Assimp;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private const int Width=800;
    private const int Height=600;

    private static readonly bool EnableValidationLayers = true;


    private static readonly string[] validationLayers = {
        "VK_LAYER_KHRONOS_validation"
    };
    private static readonly string[] deviceExtensions = {
        KhrSwapchain.ExtensionName,
#if true//mac
            "VK_KHR_portability_subset"
#endif
    };

    public static IWindow? window;
    public static Vk vk=null!;

    private static Instance instance;

    private static ExtDebugUtils? debugUtils;
    private static DebugUtilsMessengerEXT debugMessenger;


    private static Queue graphicsQueue;
    private static Queue presentQueue;
    
    private static KhrSurface? khrSurface;
    private static SurfaceKHR surface;
    private static KhrSwapchain? khrSwapChain;
    private static SwapchainKHR swapChain;
    private static Image[]? swapChainImages;
    private static ImageView[]? swapChainImageViews;
    private static Format swapChainImageFormat;
    private static Extent2D swapChainExtent;
    private static Framebuffer[]? swapChainFramebuffers;
    
    public static RenderPass RenderPass;
    private static CommandPool CommandPool;
    private static CommandBuffer[] CommandBuffers = null!;
    
    private static Image depthImage;
    private static DeviceMemory depthImageMemory;
    private static ImageView depthImageView;

    private static PhysicalDevice physicalDevice;
    public static Device device;
    private static DescriptorSetLayout DescriptorSetLayout;
    private static PipelineLayout PipelineLayout;
    private static Pipeline GraphicsPipeline;

    
    const int MAX_FRAMES_IN_FLIGHT = 2;
    public static int CurrentFrame = 0;
    static int CurrentFrameIndex = 0;
    static bool FramebufferResized = false;
    
    private static Buffer VertexBuffer;
    private static DeviceMemory VertexBufferMemory;

    public static void InitializeRenderer()
    {
        InitWindow();
        LoadMesh();
        InitVulkan();
    }

    public static void Render()
    {
        DrawFrame();
    }
    public static void Update()
    {
        CurrentFrame++;
        CurrentFrameIndex =CurrentFrame% MAX_FRAMES_IN_FLIGHT;
    }


    private static unsafe void LoadMesh()
    {
        using var assimp = Assimp.GetApi()!;
        
        var scene=assimp.ImportFile("../../../models/model.obj", (uint)PostProcessPreset.TargetRealTimeMaximumQuality)!;
        
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