using VulkanEngine.Renderer.GPUStructs;

namespace VulkanEngine.Renderer;

public static class GPURenderRegistry
{
    public static List<MeshData> Meshes = new();
    // public static List<RenderObject> RenderObjects = new();
    private static int meshInfoBufferSize = 0;
    // public static void RegisterRenderObject(RenderObject renderObject)
    // {
        // RenderObjects.Add(renderObject);
        // renderObject.RenderManagerROIndex = RenderObjects.Count - 1;
    // }
    
    public static unsafe Mesh_ref RegisterMesh(MeshData mesh)
    {
        Meshes.Add(mesh);
        VKRender.EnsureMeshRelatedBuffersAreSized();
        var vertexOffset = LoadVertices(mesh.vertexBuffer);
        var indexOffset = LoadIndices(mesh.indexBuffer);
        Meshes[^1] = Meshes[^1] with
        {
            vertexBufferOffset = (int) vertexOffset,
            indexBufferOffset = indexOffset,
        };
        for (int i = 0; i < Meshes.Count; i++)
        {
            var me = Meshes[i];
            ((MeshInfo*) VKRender.GlobalData.MeshInfoBufferPtr)[i] = new()
            {
                IBOoffset = me.indexBufferOffset,
                IBOsize = (uint) me.indexBuffer.Length,
                padding = i,
                vertexLoadOffset = (uint) me.vertexBufferOffset,
            };
        }
        
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


    // public static unsafe void WriteOutObjectData(Span<GPUStructs.ComputeInput> target)
    // {
    //     if (target.Length<RenderObjects.Count)
    //     {
    //         throw new Exception("Not enough space in buffer");
    //     }
    //     for (var i = 0; i < RenderObjects.Count; i++)
    //     {
    //         var renderObject = RenderObjects[i];
    //         target[i] = new ComputeInput()
    //         {
    //             transform=
    //                 renderObject.transform.LocalToWorldMatrix,
    //                 // float4x4.Identity,
    //             meshID = (uint) renderObject.mesh.index,
    //             materialID = (uint) renderObject.material.index,
    //         };
    //     }
    // }
    
    
}

public struct MeshData
{
    public GpuLoadStatus status;
    public uint[] indexBuffer;
    public Vertex[] vertexBuffer;
    public uint indexBufferOffset;
    public int vertexBufferOffset;
}
public enum GpuLoadStatus
{
    Failed=default,
    Unloaded,
    Loaded,
    Unloading,
    Loading,
    
}

public struct Material_ref
{
    public int index;
}
public struct Mesh_ref
{
    public int index;    
}
// public class Mesh_internal
// {
//     public string name;
//     public int indexCount;
//     public int vertexCount;
//     public ulong indexBufferOffset;
//     public long vertexBufferOffset;
//     public uint[] indexBuffer;
//     public Vertex[] vertexBuffer;
//
// }