// See https://aka.ms/new-console-template for more information

using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using Vortice.Vulkan;
using VulkanEngine;

static class Program
{
    public static string[] args=null!;
    static unsafe void Main(string[] args)
    {
        //AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        Program.args = args;
        uint a;
        Vulkan.vkInitialize().Expect();
        // Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
        Console.OutputEncoding=Encoding.UTF8;
        Console.WriteLine("Hello, World!");
        try
        {
            EngineStart.StartEngine();

        }
        catch (AccessViolationException e)
        {
            throw;
        }
        Thread.Sleep(5000);
        // throw new NotImplementedException();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Console.WriteLine(e.ExceptionObject);
        
    }
}
