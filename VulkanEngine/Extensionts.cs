#define ASSERTS
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Vulkan;
using Range = System.Range;
using Result=Vortice.Vulkan.VkResult;

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

    public static unsafe T* ptr<T>(this ref T a) where T : unmanaged
    {
        return (T*)Unsafe.AsPointer(ref a);
    }
    public static unsafe T* ptr<T>(this Span<T> a) where T : unmanaged
    {
        return (T*) Unsafe.AsPointer(ref a.GetPinnableReference());
    }

    public static unsafe T* ptr<T>(this T[] a) where T : unmanaged
    {
        return (T*) Unsafe.AsPointer(ref a[0]);
    }


    public static int2 ToInt2(this VkExtent2D extent) => new((int)extent.width,(int)extent.height);
    
    
    public delegate void ActionRef<T>(ref T item);
    public delegate void ActionRefIndexed<T>(ref T item,int i);
    
    public static void ForEachRef<T>(this Span<T> list, ActionRefIndexed<T> action)
    {
        for (var i = 0; i < list.Length; i++)
        {
            action(ref list[i],i);
        }
    }
    public static void ForEachRef<T>(this T[] list, ActionRefIndexed<T> action)
    {
        for (var i = 0; i < list.Length; i++)
        {
            action(ref list[i],i);
        }
    }
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


    [Conditional("ASSERTS"),Conditional("DEBUG")]
    public static void Assert(this bool b,string message="assertion failed")
    {
        if (!b)
        {
            throw new(message);
        }
    }
    [Conditional("ASSERTS"),Conditional("DEBUG")]
    public static void AssertLog(this bool b,string message="assertion failed")
    {
        if (!b)
        {
            Console.Write("Assert:");
            Console.WriteLine(message);
        }
    }

    public static unsafe bool BitwiseEquals<T>(ref this T lhs,ref T rhs) where T : unmanaged
    {
        var a = new ReadOnlySpan<byte>(Unsafe.AsPointer(ref lhs), Unsafe.SizeOf<T>());
        return a.SequenceEqual(new ReadOnlySpan<byte>(Unsafe.AsPointer(ref rhs), Unsafe.SizeOf<T>()));
    }

    public static uint SizeInBytes<T>(this Image<T> image) where T : unmanaged, IPixel<T>
    {
        return (uint) (image.Height * image.Width * image.PixelType.BitsPerPixel/8);
    }

    public static (int start, int end) Decompose(this Range range) => (range.Start.Value, range.End.Value);
    public static bool DoesOverlap(this Range lhs,Range rhs) => 
        Math.Max(lhs.Start.Value, lhs.End.Value) > Math.Max(rhs.Start.Value, rhs.End.Value)
            ? Math.Max(rhs.Start.Value, rhs.End.Value) >= Math.Min(lhs.Start.Value, lhs.End.Value)
            : Math.Max(lhs.Start.Value, lhs.End.Value) >= Math.Min(rhs.Start.Value, rhs.End.Value);

    public static Range Consolidate(this Range lhs, Range rhs) =>
        (Math.Min(Math.Min(lhs.Start.Value, lhs.End.Value), Math.Min(rhs.Start.Value, rhs.End.Value))..Math.Max(Math.Max(lhs.Start.Value, lhs.End.Value), Math.Max(rhs.Start.Value, rhs.End.Value)));
    
    private static Range Normalize(this Range range) =>
        (Math.Min(range.Start.Value, range.End.Value)..Math.Max(range.Start.Value, range.End.Value));
}