using Silk.NET.Maths;
using Silk.NET.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using VulkanEngine.Renderer.Internal;

namespace VulkanEngine.Renderer;

public static class RenderManager
{
    public static List<Mesh_internal> Meshes = new();
    public static List<RenderObject> RenderObjects = new();
    private static int meshInfoBufferSize = 0;
    public static void RegisterRenderObject(RenderObject renderObject)
    {
        RenderObjects.Add(renderObject);
    }
    


    public static unsafe void WriteOutObjectData(Span<GPUStructs.ComputeInput> target)
    {
        if (target.Length<RenderObjects.Count)
        {
            throw new Exception("Not enough space in buffer");
        }
        for (var i = 0; i < RenderObjects.Count; i++)
        {
            var renderObject = RenderObjects[i];
            target[i] = new ComputeInput()
            {
                transform=renderObject.transform.ModelMatrix,
                meshID = (uint) renderObject.mesh.index,
                materialID = (uint) renderObject.material.index
            };
        }
    }
    
}
public struct RenderObject_internal
{
    public float3 position;
    public Quaternion<float> rotation;
    public float3 scale;
    public int mesh;
    public int material;
}
public struct Material_ref
{
    public int index;    
}
public struct Mesh_ref
{
    public int index;    
}
