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
using VulkanEngine.Renderer.Internal;

namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    static Camera2 camera = new();
    static FPSCounter fpsCounter = new(100000);
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




        var CameraTarget = CreateEntity();
        TransformSystem.AddItemWithGlobalID(CameraTarget);
        var Cam = CreateEntity();
        TransformSystem.AddItemWithGlobalID(Cam);
        // Camera2


        var mesh_ref=RenderManager.RegisterMesh(
            new Mesh_internal()
            {
                name = "demo monkey", indexBuffer = VKRender.indices, vertexBuffer = VKRender.vertices,
                indexCount = VKRender.indices.Length, vertexCount = VKRender.vertices.Length
            }
        );
        monkey1 = new RenderObject(new Transform(new(0,0,1),Quaternion<float>.Identity, float3.One),new(){index = 0},new(){index = 0});
        RenderManager.RegisterRenderObject(monkey1);
        monkey2 = new RenderObject(new Transform(new(0,0,0),Quaternion<float>.Identity, float3.One),new(){index = 0},new(){index = 0});
        RenderManager.RegisterRenderObject(monkey2);

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

         new[]
             {
                 "*.vert",
                 "*.frag",
                 "*.comp",
             }.SelectMany(search_string => Directory.GetFiles(VKRender.AssetsPath + "/shaders",
                 search_string,
                 SearchOption.AllDirectories))
             .Select(in_name =>
             {
                 
                 var out_name = VKRender.AssetsPath + "/shaders/compiled"+in_name[((VKRender.AssetsPath + "/shaders").Length)..]+".spv";

                 return Process.Start("glslc", $@"{in_name} -o {out_name}");
             }).
             ForEach(
                 x => x!.WaitForExit()
                 );

    }

   


    
 
}