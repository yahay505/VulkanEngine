using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace VulkanEngine.Renderer;
[StructLayout(LayoutKind.Sequential, Size = 192, Pack = 1)]
public struct UniformBufferObject
{
    public Matrix4X4<float> model;
    public Matrix4X4<float> view;
    public Matrix4X4<float> proj;
}
