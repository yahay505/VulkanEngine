// using System.Diagnostics;
// using System.Runtime.CompilerServices;
// using Silk.NET.Maths;
// using VulkanEngine.ECS_internals;
// using VulkanEngine.Phases.FramePreamblePhase;
// using VulkanEngine.Phases.FramePreRenderPhase;
// using VulkanEngine.Phases.FramePreTickPhase;
// using VulkanEngine.Phases.FrameRender;
// using VulkanEngine.Phases.Tick;
// using VulkanEngine.Renderer.ECS;
// using VulkanEngine.Renderer2;
// using VulkanEngine.Renderer2.Text;
//
// namespace VulkanEngine;
// using VulkanEngine.Renderer;
//
// public static class Game
// {
//     private static RenderObject monkey1;
//     private static RenderObject monkey2;
//     //public static IInputContext InputCntx;
//
//     public static void Run()
//     {
//         
//         
//         var CameraTarget = CreateEntity();
//         var CamTargetTransform=TransformSystem.AddItemWithGlobalID(CameraTarget);
//         
//         var Cam = CreateEntity();
//         var CamTransform = TransformSystem.AddItemWithGlobalID(Cam);
//         CameraData._data.AddItemWithGlobalID(Cam,new(){farPlaneDistance = 1000f,nearPlaneDistance = 0.01f,fov = 60});
//         CamTransform.parent = CamTargetTransform;
//         
//         CamTransform.local_position = new float3(0,0,0);
//         CamTransform.local_position = new float3(10,0,0);    
//
//         CamTransform.local_rotation = Quaternion<float>.CreateFromYawPitchRoll(0, 0,float.Pi/2f);
//         
//         DebugTextRenderer.init();
//
//         
//         
//         var meshes = Renderer2.infra.Config.VKRender.LoadMesh(Renderer2.infra.Config.VKRender.AssetsPath + "models/scene.glb");
//         var refs=meshes.Select((mesh)=>GPURenderRegistry.RegisterMesh(new(){indexBuffer = mesh.indices,vertexBuffer = mesh.vertices})).ToArray();
//
//         int[] staticScene = new int[meshes.Length];
//         // load scene
//         for (var i = 0; i < meshes.Length; i++)
//         {
//             var mesh = meshes[i];
//             staticScene[i] = CreateEntity();
//             
//             var transform = TransformSystem.AddItemWithGlobalID(staticScene[i]);
//             Silk.NET.Maths.Matrix4X4.Decompose(Matrix4X4.Transpose(mesh.transform), out var scale, out var rot, out var pos);
//             transform.local_scale = scale;
//             transform.local_rotation = rot;
//             transform.local_position = pos;
//             Console.WriteLine($"{i} :: pos: {transform.local_position} rot: {transform.local_rotation.ToEuler()*180f/float.Pi} scale: {transform.local_scale} parent: {mesh.parentID}");
//             // if (mesh.parentID>=0)
//                 // transform.parent = Unsafe.BitCast<int,Transform_ref>(TransformSystem._data.EntityIndices.Span[staticScene[mesh.parentID]]);
//             MeshComponent._data.AddItemWithGlobalID(staticScene[i],new(){registryMeshID = refs[i].index});
//         }
//         
//         // load monkey
//         // var (vertices, indices,_,_) = VKRender.LoadMesh(VKRender.AssetsPath + "/models/model.obj").Single();
//         // var monkeyMeshRef = GPURenderRegistry.RegisterMesh(new(){indexBuffer = indices,vertexBuffer = vertices});
//
//         // var monkey = CreateEntity();
//         // var trans = TransformSystem.AddItemWithGlobalID(monkey);
//         // var meshcomp= MeshComponent._data.AddItemWithGlobalID(monkey,new(){registryMeshID = monkeyMeshRef.index});
//         
//         PreambleSequence.Register();
//         FramePreTickSequence.Register();
//         FramePreRenderSequence.Register();
//         FrameRenderSequence.Register();
//         MockTickSequence.Register();
//         
//         
//         
//         
//         Scheduler.Run(Gameloop());
//         
//     }
//
//     static IEnumerable<string> LoadTestLoop()
//     {
//         var stopwatch = Stopwatch.StartNew();
//         yield return "LOADTEST";
//         stopwatch.Stop();
//         Console.WriteLine($"LoadTestLoop took {stopwatch.ElapsedMilliseconds}ms");
//     }
//
//     public static long calcMS = 0;
//     static IEnumerable<string> Gameloop()
//     {
//         while (true)
//         {
//             yield return "frame_preamble";
//             Volatile.Write(ref calcMS, Stopwatch.GetTimestamp());
//             // yield return "framePreTick";
//             yield return "tick";
//             
//             // yield return "tick";
//             // yield return "tick";
//             yield return "framePreRender";
//             yield return "frame_render";
//         }
//         yield return "LOADTEST";
//         // Console.WriteLine($"LoadTestLoop took {stopwatch.ElapsedMilliseconds}ms");
//     }
//
//
//     
//  
// }