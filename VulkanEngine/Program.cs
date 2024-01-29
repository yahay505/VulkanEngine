// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using VulkanEngine;

static class Program
{
    static void Main()
    {
        // Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Console.WriteLine("Hello, World!");
        Game.Run();

    }
}
