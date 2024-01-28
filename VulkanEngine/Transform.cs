using System.Diagnostics;
using Silk.NET.Maths;

namespace VulkanEngine;

public class Transform
{
    public static Matrix4X4<float> world_matrix = Matrix4X4.CreateWorld(float3.Zero, float3.UnitY, float3.UnitZ);
    public Transform? parent;
    public List<Transform> children = new();

    #region API

    public float3 position
    {
        get
        {
            EnsureValidity();
            return _position_cache;
        }
        set=>setPosition(value);
    }

    public Quaternion<float> rotation
    {
        get
        {
            EnsureValidity();
            return _rotation_cache;
        }
        set => setRotation(value);
    }

    public float3 scale
    {
        get
        {
            EnsureValidity();
            return _scale_cache;
        }
        set => setScale(value);
    }

    #endregion

    #region Locals

    private float3 local_position
    {
        get => _local_position;
        set
        {
            DirtyBelow();
            _local_position = value;
        }
    }
    public Quaternion<float> local_rotation
    {
        get => _local_rotation;
        set
        {
            DirtyBelow();
            _local_rotation = value;
        }
    }
    private float3 local_scale
    {
        get => _local_scale;
        set
        {
            DirtyBelow();
            _local_scale = value;
        }
    }

    #endregion

    #region Caches
        private float3 _position_cache;
        private float3 _local_position;
        private Quaternion<float> _rotation_cache;
        private Quaternion<float> _local_rotation;
        private float3 _scale_cache;
        private float3 _local_scale;
        #endregion
        


    public bool dirty = true;

    public float4x4 LocalToWorldMatrix;
    public void setPosition(float3 value)
    {
        //broken transform local to world position
        if (parent != null)
        {
            // local_position = ;
        }
        else
        {
            local_position = value;
        }
    }
    public void setRotation(Quaternion<float> value)
    {
        if (parent != null)
        {
            // local_rotation = ;
        }
        else
        {
            local_rotation = value;
        }
    }
    public void setScale(float3 value)
    {
        if (parent != null)
        {
            // local_scale = ;
        }
        else
        {
            local_scale = value;
        }
    }
    private void DirtyBelow()
    {
        if (!dirty)
            children.ForEach(a =>
            {
                a.dirty = true;
                a.DirtyBelow(); 
            });
    }

    private void EnsureValidity()
    {
        if(!dirty)
            return;
        parent?.EnsureValidity();

        if (parent != null)
        {
            _position_cache = Vector3D.Transform(local_position, parent.LocalToWorldMatrix);
            // _rotation_cache = Matrix4X4.;
            // _scale_cache = parent.scale * local_scale;
            LocalToWorldMatrix = parent.LocalToWorldMatrix * CreateParentToChildSpaceMatrix();
        }
        else
        {
            // _position_cache = local_position;
            // _rotation_cache = local_rotation;
            // _scale_cache = local_scale;
            LocalToWorldMatrix = CreateParentToChildSpaceMatrix();
        }
        dirty = false;
    }

    #region Constructors

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
    public float4x4 CreateParentToChildSpaceMatrix()
    {
        return
            (((
               Matrix4X4.CreateScale(local_scale)) *
              Matrix4X4.CreateFromQuaternion(local_rotation)) *
             Matrix4X4.CreateTranslation(local_position));
    }
    

    #endregion

    public float3 right => new(LocalToWorldMatrix.M11,LocalToWorldMatrix.M12,LocalToWorldMatrix.M13);
    public float3 forward => new(LocalToWorldMatrix.M21,LocalToWorldMatrix.M22,LocalToWorldMatrix.M23);
    public float3 up => new (LocalToWorldMatrix.M31,LocalToWorldMatrix.M32,LocalToWorldMatrix.M33);
 

    public static void ZZZZ()
    {
        var trans=new Transform(float3.One, Silk.NET.Maths.Quaternion<float>.CreateFromAxisAngle(float3.UnitZ,Single.Pi/2f), 2*float3.One);
        unsafe
        {
            Console.WriteLine(sizeof(Transform));
        }
        var p2cm=trans.CreateParentToChildSpaceMatrix();
        var test = new float3(1, 2, 3);
        var result = Vector3D.Transform(test, p2cm);
        Console.WriteLine($"{test} -> {result}");
        Console.WriteLine($"row1: {p2cm.Row1}\n row2: {p2cm.Row2}\n row3: {p2cm.Row3}\n row4: {p2cm.Row4}\n");
        var a = Matrix4X4.CreateScale(1, 2, 3f);
        var rot = Quaternion<float>.CreateFromYawPitchRoll((float) (Math.PI/2), (float) (Math.PI/2), (float) (Math.PI/2));
        var b = Matrix4X4.CreateFromQuaternion(rot);
        var c = Matrix4X4.CreateTranslation(1f, 2f, 3f);
        var total = a * b * c;
        Console.WriteLine(total);
        var vec = new float4(1, 2, 3, 1);
        Console.WriteLine(vec * total);
        // extract scale from combined transform matrix
        var scale = new float3(total.M11, total.M22, total.M33);
        // extract rotation from total matrix
        var rotation = Quaternion<float>.CreateFromRotationMatrix(total);
        // extract translation from total matrix
        var translation = new float3(total.M41, total.M42, total.M43);
        Console.WriteLine(scale);
        Console.WriteLine(rotation);
        Console.WriteLine(translation);
        // DecomposeMatrix(ref total, out var translation2, out var rotation2, out var scale2);
        // Console.WriteLine(scale2);
        // Console.WriteLine(rotation2);
        // Console.WriteLine(translation2);
        Silk.NET.Maths.Matrix4X4.Decompose(total, out var scale3, out var rotation3, out var translation3);
        Console.WriteLine(scale3);
        Console.WriteLine(rotation3);
        Console.WriteLine(translation3);
        var stp = Stopwatch.StartNew();
        var totn = total;
        float3 s, t;
        Quaternion<float> r;
        float off = 0.0001f;
        for (int i = 0; i < 1000000; i++)
        {
            Silk.NET.Maths.Matrix4X4.Decompose(totn, out s, out r, out t);
            totn = Silk.NET.Maths.Matrix4X4.CreateScale(s) * Silk.NET.Maths.Matrix4X4.CreateFromQuaternion(r) *  Silk.NET.Maths.Matrix4X4.CreateTranslation(t);
            if (MathF.Abs((s.Y)/2-1)>off)
            {
                off *= 10;
                Console.WriteLine($"off %{off*100} @ {i} => {MathF.Floor(i / 60f / 60f)}h {((i / 60f / 60f) - MathF.Floor(i / 60f / 60f))*60}m");
            }
        }
        stp.Stop();
        Console.WriteLine(stp.ElapsedMilliseconds);

    }
}

