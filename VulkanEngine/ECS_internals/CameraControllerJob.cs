using System.Numerics;
using System.Runtime.CompilerServices;
using OSBindingTMP;
using Silk.NET.Maths;
// using ImGuiNET;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreRenderPhase;

public static class CameraControllerJob
{
    
    public static float CameraRotSpeed = 0.5f;
    public static bool CameraControlEnabled = true;
    public static void CameraControl()
    {
        
        // if (Input.Input.MouseButton(0))
        // {
        //     CameraControlEnabled = true;
        // }
        //
        // if (Input.Input.Key(Key.Escape))
        // {
        //     CameraControlEnabled = false;
        // }

        CameraControlEnabled = Input.Input.MouseButton(0);
        
        
        bool _shouldControlCamera = CameraControlEnabled
                                    // &&!ImGui.GetIO().WantCaptureMouse&&!ImGui.GetIO().WantCaptureKeyboard
                                    ;

        var query = MakeQuery<Transform_ref, Camera_ref>();
        while (HasResults(ref query, out _, out var camTransform, out _))
        {
            var move = float3.Zero;
            Quaternion<float> q = Quaternion<float>.Identity;
            if (_shouldControlCamera)
            {
                const float movespeed = 1f;
                if (Input.Input.Key(layout_keys.kVK_ANSI_W))
                {
                    move += camTransform.forward * movespeed * VKRender.deltaTime;
                }
                
                if (Input.Input.Key(layout_keys.kVK_ANSI_S))
                {
                    move -= camTransform.forward * movespeed * VKRender.deltaTime;
                }
                
                if (Input.Input.Key(layout_keys.kVK_ANSI_A))
                {
                    move -= camTransform.right * movespeed * VKRender.deltaTime;
                }
                
                if (Input.Input.Key(layout_keys.kVK_ANSI_D))
                {
                    move += camTransform.right * movespeed * VKRender.deltaTime;
                }
                
                if (Input.Input.Key(layout_keys.kVK_Space))
                {
                    move += camTransform.up * movespeed * VKRender.deltaTime;
                }
                
                if (Input.Input.Key(layout_keys.kVK_Shift))
                {
                    move -= camTransform.up * movespeed * VKRender.deltaTime;
                }
                // q = Quaternion<float>.CreateFromYawPitchRoll(0, -delta.Y,-delta.X );
            
            }
            var delta = _shouldControlCamera?Input.Input.mouseDelta*CameraRotSpeed:new();
        
            // ImGui.Begin("SetCamera");
            var camPosition = camTransform.world_position;
            // var camRotation = camTransform.world_rotation;
            var camRotationDegree = (camTransform.world_rotation).ToEuler()*180/MathF.PI;
            // var loc_camRotationDegree = (camTransform.local_rotation).ToEuler()*180/MathF.PI;
            var camScale = camTransform.world_scale;
            unsafe
            {
                // ImGui.InputFloat3("pos",ref Unsafe.AsRef<Vector3>((Vector3*)&camPosition),format:"%.3f");
                // ImGui.InputFloat3("rot",ref Unsafe.AsRef<Vector3>((Vector3*)&camRotationDegree),format:"%.3f");
                // // ImGui.InputFloat3("loc_rot",ref Unsafe.AsRef<Vector3>((Vector3*)&loc_camRotationDegree),format:"%.3f");
                // ImGui.InputFloat3("scale",ref Unsafe.AsRef<Vector3>((Vector3*)&camScale),format:"%.3f");
            }
            camRotationDegree = new float3(camRotationDegree.X-delta.Y,0f,camRotationDegree.Z-delta.X);
            // camRotationDegree *= Single.Pi / 180f;
            camTransform.world_rotation = (camRotationDegree/180f*MathF.PI).ToQuaternion();
            // camTransform.local_rotation = Quaternion<float>.CreateFromYawPitchRoll(loc_camRotationDegree.Y,loc_camRotationDegree.X,loc_camRotationDegree.Z);
            // camTransform.local_rotation = (loc_camRotationDegree/180f*float.Pi).ToQuaternion();
        
            camTransform.world_position = camPosition + move;
            // camTransform.world_position = camPosition;
        
            // ImGui.Text($"Camera Position: {camTransform.world_position :F3}");
            // ImGui.Text($"Camera Rotation: {camTransform.world_rotation.ToEuler()*180/MathF.PI:F3}");
            // ImGui.End();
            // Thread.Sleep(100);
            break;
        
            
        }
    }
}