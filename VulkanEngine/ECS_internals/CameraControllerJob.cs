using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class CameraControllerJob
{
    
    public static float CameraSpeed = 0.1f;
    public static void CameraControl()
    {
        unsafe
        {
            var query = MakeQuery<Transform_ref, VulkanEngine.Camera2>();
            while (HasResults(ref query, out _, out var transform, out _))
            {
                // rotate camera
                var delta = Input.Input.mouseDelta*CameraSpeed;
                Quaternion<float> q = Quaternion<float>.CreateFromYawPitchRoll(delta.X, delta.Y, 0);
                var transformRef = transform.parent;
                transformRef.local_rotation *= q;
                break;
            }
        }
    }
}