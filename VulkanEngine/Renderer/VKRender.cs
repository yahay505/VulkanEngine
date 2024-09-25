using System.Numerics;
using System.Reflection;
using Silk.NET.Assimp;
using Silk.NET.Maths;
using Silk.NET.Vulkan.Extensions.ImGui;
using Vortice.Vulkan;


namespace VulkanEngine.Renderer;

public static partial class VKRender
{

    private static bool DrawIndirectCountAvaliable =>
        true
        // || DeviceInfo.supportsCmdDrawIndexedIndirectCount
        // false
        ;
    

    // private const int Width=800;
    // private const int Height=600;

    public static DeviceInfo DeviceInfo = null!;
    // public static IWindow window = null!;
    // public static Vk vk = Vk.GetApi()!;

    private static VkInstance instance;

    private static VkDebugUtilsMessengerEXT debugMessenger;

    private static VkQueue transferQueue;
    private static VkQueue computeQueue;
    private static VkQueue graphicsQueue;
    private static VkQueue presentQueue;

    public static EngineWindow mainWindow = null!;
    
    //
    // private static List<EngineWindow> windows => null!;
    // private static EngineWindow activeWindow => null!;

    // private static SurfaceKHR surface =>mainWindow.surface;
    // private static SwapchainKHR swapChain =>mainWindow.swapChain;
    // private static Image[] swapChainImages =>mainWindow.;
    // private static ImageView[] swapChainImageViews=null!;
    // private static Format swapChainImageFormat;
    // private static VkExtent2D swapChainExtent;
    // private static Framebuffer[]? swapChainFramebuffers;
    
    public static VkRenderPass RenderPass;


    public static VkPhysicalDevice physicalDevice=>DeviceInfo.device;
    public static VkDevice device;
    public static VkDescriptorSetLayout DescriptorSetLayout;
    public static VkPipelineLayout GfxPipelineLayout;
    public static VkPipeline GraphicsPipeline;

    public static VkPipeline ComputePipeline;
    public static VkPipelineLayout ComputePipelineLayout;
    private static VkDescriptorSetLayout ComputeDescriptorSetLayout;
    static VkDescriptorPool DescriptorPool;


    public static int CurrentFrame = 0;
    public static int CurrentFrameIndex = 0;
    static bool FramebufferResized = false;
    
    public static FrameData[] FrameData = null!;
    public static ref FrameData GetCurrentFrame()=> ref FrameData[CurrentFrameIndex];
    public static FrameData GetLastFrame()=> FrameData[(CurrentFrameIndex+FRAME_OVERLAP-1)%FRAME_OVERLAP];
    public static Action[] FrameCleanup = null!;
    
    public static Camera currentCamera = new();
    public static void RegisterActionOnAllOtherFrames(Action cleanup)
    {
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            if (i==CurrentFrameIndex) continue;
            FrameCleanup[i]+=cleanup;
        }
    }
    //public static ImGuiController imGuiController = null!;


    public static void Render()
    {
        bool retry;
        do
        {
            retry = false;
            DrawFrame(mainWindow, out retry);
            CurrentFrame++;
            CurrentFrameIndex =CurrentFrame% FRAME_OVERLAP;
        } while (false);
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
        _RPath=f.FullName+"/Assets/";
        return _RPath;
    }
}


private static string _RPath;

public static void ExecuteCleanupScheduledForCurrentFrame()
{
    FrameCleanup[CurrentFrameIndex]();
    FrameCleanup[CurrentFrameIndex]=()=>{};
}
public struct Camera
{
    public float4x4 view;
    public float4x4 proj;
        
}

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
                a.Add((_vertices.ToArray(), _indices.ToArray(), (ma * node->MTransformation).ToGeneric(),parentID));
            }
        
            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]!,id,ma);
            }
        }
    }
}