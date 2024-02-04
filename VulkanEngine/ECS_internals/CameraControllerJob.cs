using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class CameraControllerJob
{
    
    public static float CameraSpeed = 0.01f;
    public static void CameraControl()
    {
        if (Input.Input.MouseButton(0))
        {
            return;
        }
        var query = MakeQuery<Transform_ref, Camera_ref>();
        while (HasResults(ref query, out _, out var transform, out _))
        {
            // rotate camera
            var delta = Input.Input.mousePosition*CameraSpeed;
            Quaternion<float> q = Quaternion<float>.CreateFromYawPitchRoll(0, delta.Y, delta.X);
            var transformRef = transform.parent;
            transformRef.local_rotation = q;
            break;
        }
    }
}