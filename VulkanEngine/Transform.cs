using Silk.NET.Maths;

namespace VulkanEngine;

public struct Transform
{
    public float3 position;
    public Quaternion<float> rotation;
    public float3 scale;
    public Transform(float3 position, Quaternion<float> rotation, float3 scale)
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
    }
    public Transform(float3 position, float3 rotation, float3 scale)
    {
        this.position = position;
        this.rotation = Quaternion<float>.CreateFromYawPitchRoll(rotation.Z,rotation.X,rotation.Y);
        this.scale = scale;
    }
    
    public float3 up => Vector3D.Transform( new float3(0, 0, 1),rotation);
    public float3 right => Vector3D.Transform( new float3(1, 0, 0),rotation);
    public float3 forward => Vector3D.Transform( new float3(0, 1, 0),rotation);
    public float4x4 Matrix => Matrix4X4.CreateFromQuaternion(rotation) * Matrix4X4.CreateScale(scale) * Matrix4X4.CreateTranslation(position);
    public float4x4 ViewMatrix => Matrix4X4.CreateTranslation(-position) * Matrix4X4.CreateFromQuaternion(rotation);
    
    
}

