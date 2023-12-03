namespace VulkanEngine.Renderer;

public class RenderObject
{
    public Transform transform;
    public Mesh_ref mesh;
    public Material_ref material;
    public int RenderManagerROIndex;
    public RenderObject(Transform transform, Mesh_ref mesh, Material_ref material)
    {
        this.transform = transform;
        this.mesh = mesh;
        this.material = material;
    }
    
}