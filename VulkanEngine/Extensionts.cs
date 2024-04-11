#define ASSERTS
using System.Diagnostics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using Range = System.Range;

namespace VulkanEngine;

public static class Extensions
{
    [DebuggerHidden,DebuggerStepThrough]
    public static void Expect(this Result result, string error_text="vulkan error")
    {
        if (result!= Result.Success)
        {
            throw new(error_text+" with error: "+result+"\n");
        }
    }
    public static IEnumerable<int> Times(this Range range)
    {
     //improvement: use a struct enumerator, LinqGen
        int i=range.Start.Value;
        while (i<range.End.Value)
        {
            yield return i;
            i++;
        }
    }
    
    public static int2 ToInt2(this Extent2D extent) => new((int)extent.Width,(int)extent.Height);
    
    
    public delegate void ActionRef<T>(ref T item);
    public static void ForEachRef<T>(this List<T> list, ActionRef<T> action)
    {
        var listSpan = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < list.Count; i++)
        {
            action(ref listSpan[i]);
        }
    }
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
        {
            action(item);
        }
    }
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T,int> action)
    {
        var i = 0;
        foreach (var item in enumerable)
        {
            action(item,i++);
        }
    }

    [Conditional("ASSERTS")]
    public static void Assert(this bool b,string message="assertion failed")
    {
        if (!b)
        {
            throw new(message);
        }
    }
    [Conditional("ASSERTS")]
    public static void AssertLog(this bool b,string message="assertion failed")
    {
        if (!b)
        {
            Console.Write("Assert:");
            Console.WriteLine(message);
        }
    }
}