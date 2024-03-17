using System.Numerics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class CameraControllerJob
{
    
    public static float CameraRotSpeed = 1f;
    public static bool CameraControlEnabled = true;
    public static void CameraControl()
    {
        
        if (Input.Input.MouseButton(0))
        {
            CameraControlEnabled = true;
        }

        if (Input.Input.Key(Key.Escape))
        {
            CameraControlEnabled = false;
        }
        bool _shouldControlCamera = CameraControlEnabled&&!ImGui.GetIO().WantCaptureMouse&&!ImGui.GetIO().WantCaptureKeyboard;

        var query = MakeQuery<Transform_ref, Camera_ref>();
        while (HasResults(ref query, out _, out var camTransform, out _))
        {
            var move = float3.Zero;
            // Quaternion<float> q = Quaternion<float>.Identity;
            if (_shouldControlCamera)
            {
                const float movespeed = 0.4f;
                if (Input.Input.Key(Key.W))
                {
                    move += camTransform.forward * movespeed * VKRender.deltaTime;
                }

                if (Input.Input.Key(Key.S))
                {
                    move -= camTransform.forward * movespeed * VKRender.deltaTime;
                }

                if (Input.Input.Key(Key.A))
                {
                    move -= camTransform.right * movespeed * VKRender.deltaTime;
                }

                if (Input.Input.Key(Key.D))
                {
                    move += camTransform.right * movespeed * VKRender.deltaTime;
                }

                if (Input.Input.Key(Key.Space))
                {
                    move += camTransform.up * movespeed * VKRender.deltaTime;
                }

                if (Input.Input.Key(Key.ShiftLeft))
                {
                    move -= camTransform.up * movespeed * VKRender.deltaTime;
                }
                // q = Quaternion<float>.CreateFromYawPitchRoll(0, -delta.Y,-delta.X );

            }
            var delta = Input.Input.mouseDelta*CameraRotSpeed;

            ImGui.Begin("SetCamera");
            var camPosition = camTransform.world_position;
            // var camRotation = camTransform.world_rotation;
            var camRotationDegree = (camTransform.world_rotation).ToEuler()*180/MathF.PI;
            var camScale = camTransform.world_scale;
            unsafe
            {
                ImGui.InputFloat3("pos",ref Unsafe.AsRef<Vector3>((Vector3*)&camPosition),format:"%.3f");
                ImGui.InputFloat3("rot",ref Unsafe.AsRef<Vector3>((Vector3*)&camRotationDegree),format:"%.3f");
                ImGui.InputFloat3("scale",ref Unsafe.AsRef<Vector3>((Vector3*)&camScale),format:"%.3f");
            }
            camRotationDegree = new float3(camRotationDegree.X+delta.Y,0f,camRotationDegree.Z+delta.X);
            camRotationDegree *= Single.Pi / 180f;
            camTransform.world_rotation = Quaternion<float>.CreateFromYawPitchRoll(camRotationDegree.X,camRotationDegree.Y,camRotationDegree.Z);
            

            camTransform.world_position = camPosition + move;

            ImGui.Text($"Camera Position: {camTransform.world_position}");
            ImGui.Text($"Camera Rotation: {camTransform.world_rotation.ToEuler()*180/MathF.PI}");
            ImGui.End();
            break;
        
        }
    }
}