using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class CameraControllerJob
{
    
    public static float CameraRotSpeed = 0.01f;
    public static void CameraControl()
    {
        if (!Input.Input.MouseButton(0))
        {
            return;
        }
        var query = MakeQuery<Transform_ref, Camera_ref>();
        while (HasResults(ref query, out _, out var camTransform, out _))
        {
            var move = float3.Zero;
            const float movespeed = 0.4f;
            if (Input.Input.Key(Key.W))
            {
                move += camTransform.forward*movespeed*VKRender.deltaTime;
            }
            if (Input.Input.Key(Key.S))
            {
                move -= camTransform.forward*movespeed*VKRender.deltaTime;
            }

            if (Input.Input.Key(Key.A))
            {
                move -= camTransform.right*movespeed*VKRender.deltaTime;
            }
            if (Input.Input.Key(Key.D))
            {
                move += camTransform.right*movespeed*VKRender.deltaTime;
            }
            if (Input.Input.Key(Key.Space))
            {
                move += camTransform.up*movespeed*VKRender.deltaTime;
            }
            if (Input.Input.Key(Key.ShiftLeft))
            {
                move -= camTransform.up*movespeed*VKRender.deltaTime;
            }
            
            
            
            // rotate camera
            var delta = Input.Input.mouseDelta*CameraRotSpeed;
            Quaternion<float> q = Quaternion<float>.CreateFromYawPitchRoll(0, -delta.Y,-delta.X );
            camTransform.local_position += move;
            camTransform.local_rotation *= q;
            // var camTransformParent = camTransform.parent;
            // camTransformParent.local_rotation *= q;
            // ImGui.Begin("SetCamera");
            // ImGui.Text($"Camera Position: {camTransformParent.world_position}");
            // ImGui.Text($"Camera Rotation: {camTransformParent.local_rotation}");
            // ImGui.End();
            break;
        }
    }
}