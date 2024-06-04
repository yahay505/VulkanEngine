using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    [Conditional("DEBUG")]
    public static unsafe void MarkObject<T>(T target, sbyte* name, int lname, int variant = -1) where T : struct =>
        MarkObject(target, new(name, lname), variant);

    [Conditional("DEBUG")]
    public static unsafe void MarkObject<T>(T target,ReadOnlySpan<byte> name,int variant=-1) where T : struct
    {
        var str = new VkDebugUtilsObjectNameInfoEXT()
        {
            objectType = getEnum(target),
            objectHandle = Unsafe.BitCast<T,ulong>(target),
            pObjectName = name.As<byte,sbyte>().GetPointer(),
        };
        ReadOnlySpan<byte> rname;
        if (variant!=-1)
        {
            rname = Encoding.UTF8.GetBytes($"{Encoding.UTF8.GetString(name)}-v{variant}\0");
            str.pObjectName = (sbyte*) rname.GetPointer();
        }
        vkSetDebugUtilsObjectNameEXT(device, &str);
    }
    private static VkObjectType getEnum<T>(T obj)
    {
        switch (obj)
        {
            case VkBuffer _:
                return VkObjectType.Buffer;
            case VkDeviceMemory _:
                return VkObjectType.DeviceMemory;
        }
        throw new UnreachableException();
    }
}