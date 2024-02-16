using ImGuiNET;
using Silk.NET.Maths;

namespace VulkanEngine.Renderer;

public partial class VKRender
{
    // public Mesh LoadMeshAssimp(string path)
    // {
    //     return new Mesh();
    // }
    public static void SetCamera(Transform_ref transform, VulkanEngine.CameraData camera)
    {
        // Console.WriteLine($"{transform.world_position} {transform.forward} {transform.up} {camera.fov} {camera.nearPlaneDistance} {camera.farPlaneDistance}");
        ImGui.Begin("SetCamera");
        
        currentCamera = new()
        {
            view = Matrix4X4.CreateLookAt(
                transform.world_position, 
                transform.world_position + transform.forward, 
                // new float3(0,0,0),
                transform.up),
            proj = Matrix4X4.CreatePerspectiveFieldOfView(Scalar.DegreesToRadians(camera.fov),
                (float) swapChainExtent.Width / swapChainExtent.Height, camera.nearPlaneDistance, camera.farPlaneDistance),
        };
        currentCamera.proj.M22 *= -1;
        ImGui.Text($"Camera view:\n {currentCamera.view.Row1:F3}\n{currentCamera.view.Row2:F3}\n{currentCamera.view.Row3:F3}\n{currentCamera.view.Row4:F3}");
        Matrix4X4.Decompose(currentCamera.view, out var scale, out var rotation, out var translation);
        ImGui.Text($"Decomposed view:\n {scale:F3} \n {Vector3D.Transform(float3.One,rotation)*180f/float.Pi:F3}\n {translation:F3}");
        ImGui.End();
    }
}

