using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;
using VulkanEngine.ECS_internals;
using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine;
// [Derive("SystemInit")]
public static class TransformSystem{
    public static ECSResource Resource = new ECSResource("TransformSystem");
    static TransformSystem()
    {
        ECS.RegisterSystem<TransformD>(_data,typeof(Transform_ref));
    }
    public static ComponentStorage<TransformD> _data=new(false,100);

    public static Transform_ref AddItemWithGlobalID(int ID)
    {
        var t = new TransformD();
        // t.local_to_world_matrix = float4x4.Identity;
        t.local_rotation = Quaternion<float>.Identity;
        t.local_position = float3.Zero;
        t.local_scale = float3.One;
        t.parent_id = -1;
        t.child_id = -1;
        t.sibling_id = -1;
        t.dirty = true;
        return Unsafe.BitCast<int,Transform_ref>(_data.AddItemWithGlobalID(ID,t));
    }

}
public struct Transform_ref:Iinterface
{
    public static readonly Transform_ref invalid = new Transform_ref(0);
    public readonly int id=0;
    Transform_ref(int id)
    {
        this.id = id;
    }

    public ref TransformD transform=>ref TransformSystem._data.ComponentList.Span[id];
    public float3 right => new(local_to_world_matrix.M11,local_to_world_matrix.M12,local_to_world_matrix.M13);
    public float3 forward => new(local_to_world_matrix.M21,local_to_world_matrix.M22,local_to_world_matrix.M23);
    public float3 up => new (local_to_world_matrix.M31,local_to_world_matrix.M32,local_to_world_matrix.M33);
    private static void DirtyBelow(int id)
    {
        var t = id;
        while(t!=-1)
        {
            ref var a = ref TransformSystem._data.ComponentList.Span[t];
            if (!a.dirty)
            {
                a.dirty = true;
                DirtyBelow(a.child_id);
            }
            t = a.sibling_id;
        }
    }
    private static void EnsureValidity(int id)
    {
         var t = new Transform_ref(id);
        if(!t.transform.dirty)
            return;
        if (t.transform.parent_id!=-1)
        {
            EnsureValidity(t.transform.parent_id);
            var parent = new Transform_ref(t.transform.parent_id);
            // t.local_to_world_matrix = t.CreateSRTMatrix() * parent.local_to_world_matrix;
        }
        else
        {
            // t.local_to_world_matrix = t.CreateSRTMatrix();
        }
        t.transform.dirty = false;
    }
    public float4x4 CreateSRTMatrix()
    {
        return
            (((
                  Matrix4X4.CreateScale(local_scale)) *
              Matrix4X4.CreateFromQuaternion(local_rotation)) *
             Matrix4X4.CreateTranslation(local_position));
    }
    public float3 local_position
    {
        get => transform.local_position;
        set
        {
            transform.local_position = value;
            DirtyBelow(id);
        }
    }
    public float3 local_scale
    {
        get => transform.local_scale;
        set
        {
            transform.local_scale = value;
            DirtyBelow(id);
        }
    }
    public Quaternion<float> local_rotation
    {
        get => transform.local_rotation;
        set
        {
            transform.local_rotation = value;
            DirtyBelow(id);
        }
    }
    public float4x4 local_to_world_matrix
    {
        get
        {
            if (parent.id!=-1)
            {
                return parent.CreateSRTMatrix() * CreateSRTMatrix();
            }
            return CreateSRTMatrix();
        }
    }

    public float3 world_position
    {
        get
        {
            EnsureValidity(id);
            return new float3(local_to_world_matrix.M41, local_to_world_matrix.M42,
                local_to_world_matrix.M43);
        }
        set
        {
            local_position = transform.parent_id == -1 ? value : value - new Transform_ref(transform.parent_id).world_position;
            DirtyBelow(id);
        }
    }
    public float3 world_scale
    {
        get
        {
            EnsureValidity(id);
            Matrix4X4.Decompose(local_to_world_matrix,out var scale,out _,out _);
            return scale;
        }
        set
        {
            local_scale = transform.parent_id == -1 ? value : value / new Transform_ref(transform.parent_id).world_scale;
            DirtyBelow(id);
        }
    }
    public Quaternion<float> world_rotation
    {
        get
        {
            EnsureValidity(id);
            Matrix4X4.Decompose(local_to_world_matrix,out _,out var rotation,out _);
            return rotation;
        }
        set
        {
            local_rotation = transform.parent_id == -1 ? value : value / new Transform_ref(transform.parent_id).world_rotation;
            DirtyBelow(id);
        }
    }
    
    public Transform_ref parent
    {
        get => new(transform.parent_id);
        set
        {
            var oldParent = new Transform_ref(transform.parent_id);
            if (oldParent.id == value.id)
                return;
            if (oldParent.id != -1)
            {
                if (oldParent.transform.child_id == id)
                {
                    oldParent.transform.child_id = transform.sibling_id;
                }
                else
                {
                    var t = oldParent.transform.child_id;
                    while (t != -1)
                    {
                        var child = new Transform_ref(t);
                        if (child.transform.sibling_id == id)
                        {
                            child.transform.sibling_id = transform.sibling_id;
                            break;
                        }
                        t = child.transform.sibling_id;
                    }
                }
            }
            transform.parent_id = value.id;
            transform.sibling_id = -1;
            if (value.id != -1)
            {
                if (value.transform.child_id == -1)
                {
                    value.transform.child_id = id;
                }
                else
                {
                    var t = value.transform.child_id;
                    while (t != -1)
                    {
                        var child = new Transform_ref(t);
                        if (child.transform.sibling_id == -1)
                        {
                            child.transform.sibling_id = id;
                            break;
                        }
                        t = child.transform.sibling_id;
                    }
                }
            }
            DirtyBelow(id);
        }
    }

    public Transform_ref child
    {
        get => new Transform_ref(transform.child_id);
    }
    public Transform_ref sibling
    {
        get => new Transform_ref(transform.sibling_id);
    }

    public static void zort()
    {
        throw new NotImplementedException();
    }
}
[StructLayout(LayoutKind.Sequential,Pack = 64)]
public struct TransformD:Idata
{
    // public float4x4 _local_to_world_matrix
    // {
    //     get { throw new NotImplementedException(); }
    // }

    public Quaternion<float> local_rotation;
    public float3 local_position;
    public float3 local_scale;
    public int parent_id;
    public int child_id;
    public int sibling_id;
    public bool dirty;
}
