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
            throw new(error_text+"with error: "+result+"\n");
        }
    }

    public static IEnumerable<int> Times(this Range range)
    {
        int i=range.Start.Value;
        while (i<range.End.Value)
        {
            yield return i;
            i++;
        }
    }
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }
}