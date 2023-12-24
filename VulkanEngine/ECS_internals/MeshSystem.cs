using VulkanEngine.ECS_internals;

namespace VulkanEngine;

public static class MeshSystem
{
    static MeshSystem()
    {
        ECS.RegisterSystem(data);
    }
    public static ComponentStorage<MeshData> data = new(false,100);
    public static int AddItemWithGlobalID(int ID)
    {
        throw new NotImplementedException();
    }
}
public struct MeshData:Idata
{
    public int mesh_id;
    public int material_id;
    public int texture_id;
}

