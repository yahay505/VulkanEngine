using Silk.NET.Maths;

namespace VulkanEngine;

public class Transform
{
    public Transform? parent;
    public List<Transform> children = new();
    private float3 local_position;
    public float3 position
    {
        get
        {
            if (parent != null)
            {
                return parent.position + local_position;
            }
            else
            {
                return local_position;
            }
        }
        set
        {
            if (parent != null)
            {
                local_position = value - parent.position;
            }
            else
            {
                local_position = value;
            }
        }
    }
    public Quaternion<float> rotation;
    public float3 scale;
    public Transform(float3 localPosition, Quaternion<float> rotation, float3 scale)
    {
        this.local_position = localPosition;
        this.rotation = rotation;
        this.scale = scale;
    }
    public Transform(float3 localPosition, float3 rotation, float3 scale)
    {
        this.local_position = localPosition;
        this.rotation = Quaternion<float>.CreateFromYawPitchRoll(rotation.Z,rotation.X,rotation.Y);
        this.scale = scale;
    }
    
    public float3 up => Vector3D.Transform( new float3(0, 0, 1),rotation);
    public float3 right => Vector3D.Transform( new float3(1, 0, 0),rotation);
    public float3 forward => Vector3D.Transform( new float3(0, 1, 0),rotation);
    public float4x4 ModelMatrix => Matrix4X4.CreateFromQuaternion(rotation) * Matrix4X4.CreateScale(scale) * Matrix4X4.CreateTranslation(local_position);
    // public float4x4 ViewMatrix => Matrix4X4.CreateTranslation(-local_position) * Matrix4X4.CreateFromQuaternion(rotation);


}

