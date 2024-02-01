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
        renderObject.RenderManagerROIndex = RenderObjects.Count - 1;
    }
    
    public static unsafe Mesh_ref RegisterMesh(Mesh_internal mesh)
    {
        Meshes.Add(mesh);
        VKRender.EnsureMeshRelatedBuffersAreSized();
        var vertexOffset = LoadVertices(mesh.vertexBuffer);
        var indexOffset = LoadIndices(mesh.indexBuffer);
        
        ((GPUStructs.MeshInfo*) VKRender.GlobalData.MeshInfoBufferPtr)[Meshes.Count - 1] = new()
        {
            IBOoffset = indexOffset,
            vertexLoadOffset = vertexOffset,
            IBOsize = (uint) mesh.indexBuffer.Length,
        };
        return new() {index = Meshes.Count - 1};
    }

    private static uint LoadIndices(uint[] meshIndexBuffer)
    {
        return VKRender.GlobalData.indexBuffer.Upload(meshIndexBuffer);
    }

    private static uint LoadVertices(Vertex[] meshVertexBuffer)
    {
        return VKRender.GlobalData.vertexBuffer.Upload(meshVertexBuffer);
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
                transform=renderObject.transform.CreateParentToChildSpaceMatrix(),
                meshID = (uint) renderObject.mesh.index,
                materialID = (uint) renderObject.material.index
            };
        }
    }
    
    
}

public struct Material_ref
{
    public int index;
}
public struct Mesh_ref
{
    public int index;    
}
