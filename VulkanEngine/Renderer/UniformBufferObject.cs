using System.Numerics;
using Silk.NET.Maths;

namespace VulkanEngine.Renderer;

public struct UniformBufferObject
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;
}