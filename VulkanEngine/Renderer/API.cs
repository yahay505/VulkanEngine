using Silk.NET.Maths;

namespace VulkanEngine.Renderer;

public partial class VKRender
{
    // public Mesh LoadMeshAssimp(string path)
    // {
    //     return new Mesh();
    // }
    public static void SetCamera(Transform_ref transform, VulkanEngine.Camera2 camera)
    {
        currentCamera = new()
        {
            view = Matrix4X4.CreateLookAt(transform.world_position, transform.world_position + transform.forward, transform.up),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(camera.fov),
                (float) swapChainExtent.Width / swapChainExtent.Height, camera.nearPlaneDistance, camera.farPlaneDistance),
        };
        currentCamera.proj.M22 *= -1;
    }
}

