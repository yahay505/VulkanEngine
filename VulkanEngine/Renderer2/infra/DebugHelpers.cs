using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static class GPUDEBUG
{
    [Conditional("DEBUG")]

    public static unsafe void BARRIER_ALL(this VkCommandBuffer cb)
    {
        vkCmdPipelineBarrier(cb,VkPipelineStageFlags.AllCommands,VkPipelineStageFlags.AllCommands,VkDependencyFlags.None,0,null,0,null,0,null);
    }
    [Conditional("DEBUG")]
    public static unsafe void MarkObject<T>(T target, sbyte* name, int lname, int variant = -1) where T : struct =>
        MarkObject(target, new(name, lname), variant);
    [Conditional("DEBUG")]
    public static unsafe void MarkObject<T>(T target, string s) where T : struct =>
        MarkObject(target, Encoding.UTF8.GetBytes(s), -1);
    [Conditional("DEBUG")]


    [Conditional("DEBUG")]
    public static unsafe void MarkObject<T>(T target,ReadOnlySpan<byte> name,int variant=-1) where T : struct
    {
        VkDebugUtilsObjectNameInfoEXT str = new()
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
        vkSetDebugUtilsObjectNameEXT(API.device, &str);
    }
    private static VkObjectType getEnum<T>(T obj)
    {
        switch (obj)
        {
            case VkBuffer _:
                return VkObjectType.Buffer;
            case VkDeviceMemory _:
                return VkObjectType.DeviceMemory;
            case VkImage _:
                return VkObjectType.Image;
            case VkImageView _:
                return VkObjectType.ImageView;
        }
        throw new UnreachableException();
    }
    [Conditional("DEBUG")]
    public static unsafe void BeginRegion(VkCommandBuffer commandBuffer, ReadOnlySpan<byte> name, float4 color = default)
    {
        VkDebugUtilsLabelEXT marker = new()
        {
            pLabelName = (sbyte*) name.GetPointer(),
        };
        marker.color[0] = color[0];
        marker.color[1] = color[1];
        marker.color[2] = color[2];
        marker.color[3] = color[3];
        vkCmdBeginDebugUtilsLabelEXT(commandBuffer,&marker);
        
    }

    [Conditional("DEBUG")]
    public static unsafe void EndRegion(VkCommandBuffer commandBuffer)
    {
        vkCmdEndDebugUtilsLabelEXT(commandBuffer);
    }
}