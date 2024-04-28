// See https://aka.ms/new-console-template for more information

using System.Text;
using Vortice.Vulkan;
using VulkanEngine;

static class Program
{
    public static string[] args=null!;
    static unsafe void Main(string[] args)
    {
        Program.args = args;
        uint a;
        Vulkan.vkInitialize().Expect();
        // Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine("Hello, World!");
        EngineStart.StartEngine();
    }
}
