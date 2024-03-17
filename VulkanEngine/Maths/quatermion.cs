using Silk.NET.Maths;

namespace VulkanEngine;

public static partial class Maths
{
    public static float3 ToEuler(this Quaternion<float> rotation)
    {
        // roll (x-axis rotation)
        float sinr_cosp = 2 * (rotation.W * rotation.X + rotation.Y * rotation.Z);
        float cosr_cosp = 1 - 2 * (rotation.X * rotation.X + rotation.Y * rotation.Y);
        float roll = MathF.Atan2(sinr_cosp, cosr_cosp);
        
        // pitch (y-axis rotation)
        float sinp = 2 * (rotation.W * rotation.Y - rotation.Z * rotation.X);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
        {
            pitch = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
        }
        else
        {
            pitch = MathF.Asin(sinp);
        }
        
        // yaw (z-axis rotation)
        float siny_cosp = 2 * (rotation.W * rotation.Z + rotation.X * rotation.Y);
        float cosy_cosp = 1 - 2 * (rotation.Y * rotation.Y + rotation.Z * rotation.Z);
        float yaw = MathF.Atan2(siny_cosp, cosy_cosp);
        
        return (new float3(roll, pitch, yaw));
    }
}