// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;
using VulkanEngine;

static class Program
{
    static void Main()
    {
        // Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine("Hello, World!");
        Game.Run();

    }
}
