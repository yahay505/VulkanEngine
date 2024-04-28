using VulkanEngine.Renderer;
using VulkanEngine.Renderer.ECS;

namespace VulkanEngine.Phases.FrameRender;

public static class WriteoutRenderObjects
{
    public static int writenObjectCount = 0;
    public static void WriteOutObjectData()
    {
        var query = MakeQuery<Transform_ref, MeshComponent>();
        var count = 0;
        VKRender.EnsureRenderObjectRelatedBuffersAreSized(MeshComponent._data.used);// overkill if we have meshes without transforms, but why would we???

        while (HasResults(ref query, out var id, out var transform, out var meshComponent))
        {
            VKRender.GetCurrentFrame().hostRenderObjectsBufferAsSpan[count++] = new ()
            {
                transform = transform.local_to_world_matrix,
                meshID = (uint) meshComponent.Mesh.index,
                materialID = 0,
            };
        }
        Volatile.Write(ref writenObjectCount,count);

    }
}