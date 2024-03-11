using System.Runtime.CompilerServices;
using VulkanEngine.ECS_internals;
using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.Renderer.ECS;

public struct MeshComponent:Iinterface
{
    public static readonly MeshComponent invalid = Unsafe.BitCast<int,MeshComponent>(0);
    public readonly int id;
    public static Pool<Data> _data = new Pool<Data>(false,100);
    public static ECSResource Resource = new ECSResource("MeshComponent");
    public struct Data:Idata
    {
        public int registryMeshID;
    }
    public ref Data data=>ref _data.ComponentList.Span[id];
    public Mesh_ref Mesh
    {
        get => Unsafe.BitCast<int, Mesh_ref>(data.registryMeshID);
        set => data.registryMeshID = Unsafe.BitCast<Mesh_ref, int>(value);
    }
}
