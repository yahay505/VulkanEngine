using System.Diagnostics;

namespace VulkanEngine;

public static class Debug
{
    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        Console.WriteLine(message);
    }
}