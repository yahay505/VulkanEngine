using VulkanEngine.ECS_internals;

namespace VulkanEngine;

public struct Camera2: Idata
{
    static Camera2()
    {
        ECS.RegisterSystem<Camera2>(_data);
    }
    public static ComponentStorage<Camera2> _data=new(false,2);

    public float fov;
    public float nearPlaneDistance;
    public float farPlaneDistance;
}