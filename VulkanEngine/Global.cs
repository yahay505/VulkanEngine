global using float3= Silk.NET.Maths.Vector3D<float>;
global using float2= Silk.NET.Maths.Vector2D<float>;
global using float4= Silk.NET.Maths.Vector4D<float>;
global using float4x4= Silk.NET.Maths.Matrix4X4<float>;
global using float3x3= Silk.NET.Maths.Matrix3X3<float>;

global using int2= Silk.NET.Maths.Vector2D<int>;
global using int3= Silk.NET.Maths.Vector3D<int>;
global using int4= Silk.NET.Maths.Vector4D<int>;
global using int4x4= Silk.NET.Maths.Matrix4X4<int>;
global using int3x3= Silk.NET.Maths.Matrix3X3<int>;
global using static VulkanEngine.ECS_internals.ECS;
// todo 
// IMGUI crashes if this is enabled
// vulkan Init might be leaving stack in a bad state or 
// IMGUI code might be doing something wrong
// [module: System.Runtime.CompilerServices.SkipLocalsInit]
