using System.Diagnostics;
using Silk.NET.Vulkan;

namespace VulkanEngine;

public static class Extensions
{
    [DebuggerHidden,DebuggerStepThrough]
    public static void Expect(this Result result, string error_text="vulkan error")
    {
        if (result!= Result.Success)
        {
            throw new(error_text);
        }
    }
}