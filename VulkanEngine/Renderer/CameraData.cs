using VulkanEngine.ECS_internals;

namespace VulkanEngine;

public struct CameraData: Idata
{
    static CameraData()
    {
        ECS.RegisterSystem<CameraData>(_data,typeof(Camera_ref));
    }
    public static Pool<CameraData> _data=new(false,2);

    public float fov;
    public float nearPlaneDistance;
    public float farPlaneDistance;
}
public struct Camera_ref:Iinterface
{
    // public static readonly Camera_ref invalid = new Camera_ref(0);
    public readonly int id=0;
    Camera_ref(int id)
    {
        this.id = id;
    }

    public ref CameraData data=>ref CameraData._data.ComponentList.Span[id];
}