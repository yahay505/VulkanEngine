using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Phases.FramePreamblePhase;
using VulkanEngine.Phases.FramePreRenderPhase;
using VulkanEngine.Phases.FramePreTickPhase;
using VulkanEngine.Phases.FrameRender;
using VulkanEngine.Phases.Tick;
using VulkanEngine.Renderer.ECS;

namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    private static RenderObject monkey1;
    private static RenderObject monkey2;
    public static IInputContext InputCntx;

    public static void Run()
    {
   
        
        
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


    
 
}