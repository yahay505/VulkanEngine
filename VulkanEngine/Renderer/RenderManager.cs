using VulkanEngine.Renderer.Internal;

namespace VulkanEngine.Renderer;

public static class RenderManager
{
    public static List<Mesh_internal> Meshes = new();
    public static List<RenderObject> RenderObjects = new();
    
    public static void RegisterRenderObject(RenderObject renderObject)
    {
        RenderObjects.Add(renderObject);
    }
    
}

public struct Mesh_ref
{
    private int index;    
}
