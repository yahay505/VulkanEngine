using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Phases.FramePreamblePhase;
using VulkanEngine.Phases.FramePreRenderPhase;
using VulkanEngine.Phases.FramePreTickPhase;
using VulkanEngine.Phases.FrameRender;
using VulkanEngine.Phases.Tick;
using VulkanEngine.Renderer.ECS;
using VulkanEngine.Renderer.Internal;

namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    private static RenderObject monkey1;
    private static RenderObject monkey2;
    public static IInputContext InputCntx;

    public static void Run()
    {
        foreach (var type in System.Reflection.Assembly.GetCallingAssembly().GetTypes())
        {
          RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
        
        
        MIT.Start();
        
        RegisterJobs.LoadTest(); //reference to call static constructor
        // Scheduler.Run(LoadTestLoop());

        
        
        CompileShadersTEMP();
        VKRender.InitializeRenderer(out InputCntx);
        Input.Input.Init(InputCntx);



        ECS.RegisterSystem(MeshComponent._data,typeof(MeshComponent));
        var CameraTarget = CreateEntity();
        var CamTargetTransform=TransformSystem.AddItemWithGlobalID(CameraTarget);
        
        var Cam = CreateEntity();
        var CamTransform = TransformSystem.AddItemWithGlobalID(Cam);
        CameraData._data.AddItemWithGlobalID(Cam,new(){farPlaneDistance = 1000f,nearPlaneDistance = 0.01f,fov = 60});
        CamTransform.parent = CamTargetTransform;
        
        CamTransform.local_position = new float3(0,0,0);
        CamTransform.local_position = new float3(10,0,0);    

        CamTransform.local_rotation = Quaternion<float>.Identity;
        var meshes = VKRender.LoadMesh(VKRender.AssetsPath + "/models/scene.fbx").Select((mesh)=>GPURenderRegistry.RegisterMesh(new(){indexBuffer = mesh.indices,vertexBuffer = mesh.vertices})).ToArray();

        int[] staticScene = new int[meshes.Length];
        // load scene
        for (var i = 0; i < meshes.Length; i++)
        {
            var mesh = meshes[i];
            staticScene[i] = CreateEntity();
            TransformSystem.AddItemWithGlobalID(staticScene[i]);
            MeshComponent._data.AddItemWithGlobalID(staticScene[i],new(){registryMeshID = mesh.index});
        }
        
        // load monkey
        var (vertices, indices) = VKRender.LoadMesh(VKRender.AssetsPath + "/models/model.obj").Single();
        var monkeyMeshRef = GPURenderRegistry.RegisterMesh(new(){indexBuffer = indices,vertexBuffer = vertices});

        var monkey = CreateEntity();
        var trans = TransformSystem.AddItemWithGlobalID(monkey);
        var meshcomp= MeshComponent._data.AddItemWithGlobalID(monkey,new(){registryMeshID = monkeyMeshRef.index});
        
        PreambleSequence.Register();
        FramePreTickSequence.Register();
        FramePreRenderSequence.Register();
        FrameRenderSequence.Register();
        MockTickSequence.Register();
        
        
        
        
        Scheduler.Run(Gameloop());
        
        
        
        
        VKRender.vk.DeviceWaitIdle(VKRender.device);
        VKRender.CleanUp();
    }

    static IEnumerable<string> LoadTestLoop()
    {
        var stopwatch = Stopwatch.StartNew();
        yield return "LOADTEST";
        stopwatch.Stop();
        Console.WriteLine($"LoadTestLoop took {stopwatch.ElapsedMilliseconds}ms");
    }

    public static long calcMS = 0;
    static IEnumerable<string> Gameloop()
    {
        while (true)
        {
            yield return "frame_preamble";
            Volatile.Write(ref calcMS, Stopwatch.GetTimestamp());
            // yield return "framePreTick";
            yield return "tick";
            
            // yield return "tick";
            // yield return "tick";
            yield return "framePreRender";
            yield return "frame_render";
        }
        yield return "LOADTEST";
        // Console.WriteLine($"LoadTestLoop took {stopwatch.ElapsedMilliseconds}ms");
    }
    private static void CompileShadersTEMP()
    {
        //if env has renderdoc return early
        if (Environment.GetEnvironmentVariable("RENDERDOCeee") != null)
        {
            return;
        }
        //glslc
        ///Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.vert -o vert.spv
         //   /Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.frag -o frag.spv

         var www=new[]
             {
                 "*.vert",
                 "*.frag",
                 "*.comp",
             }.SelectMany(search_string => Directory.GetFiles(VKRender.AssetsPath + "/shaders",
                 search_string,
                 SearchOption.AllDirectories))
             .Select(in_name =>
             {

                 var out_name = VKRender.AssetsPath + "/shaders/compiled" +
                                in_name[((VKRender.AssetsPath + "/shaders").Length)..] + ".spv";

                 return Process.Start("glslc", $@"{in_name} -o {out_name}");
             }).ToArray();
         // wait for all in parallel
            foreach (var process in www)
            {
                process.WaitForExit();
            }

    }

   


    
 
}