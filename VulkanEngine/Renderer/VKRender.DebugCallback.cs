using System.Diagnostics;
using System.Runtime.InteropServices;
using Pastel;
using Vortice.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    [UnmanagedCallersOnly]
    private static unsafe uint DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        var s = Marshal.PtrToStringAnsi((nint) pCallbackData->pMessage);
        if (s.StartsWith("Instance Extension:") || s.StartsWith("Device Extension:"))
        {
            return Vulkan.VK_FALSE;
        }

        if ((messageSeverity & VkDebugUtilsMessageSeverityFlagsEXT.Warning) != 0 &&
            (messageTypes & VkDebugUtilsMessageTypeFlagsEXT.Performance) == 0)
        {
            ;
        }

        if ((messageSeverity & VkDebugUtilsMessageSeverityFlagsEXT.Error) != 0)
        {
            Console.WriteLine(($"\nvalidation layer: " + s.Pastel(ConsoleColor.Red) + "\n" +
                               new StackTrace(true).ToString().Pastel(ConsoleColor.Gray)));
            ;
        }
        else
        {
            Console.WriteLine($"validation layer:" + s);
        }


//Debugger.Break();
        return Vulkan.VK_FALSE;
    }
}