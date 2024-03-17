using System.Diagnostics;
using System.Runtime.InteropServices;
using Pastel;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity,
        DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        var s = Marshal.PtrToStringAnsi((nint) pCallbackData->PMessage);
        if (s.StartsWith("Instance Extension:") || s.StartsWith("Device Extension:"))
        {
            return Vk.False;
        }

        if ((messageSeverity & DebugUtilsMessageSeverityFlagsEXT.WarningBitExt) != 0 &&
            (messageTypes & DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt) == 0)
        {
            ;
        }

        if ((messageSeverity & DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt) != 0)
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
        return Vk.False;
    }
}