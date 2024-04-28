using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OSBindingTMP;

static class Program
{
    static unsafe void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var app = MacBinding.create_application();
        var window = MacBinding.open_window("Test",
            800,
            600,
            0,
            0,
             MacBinding.NSWindowStyleMask.NSWindowStyleMaskTitled
            |MacBinding.NSWindowStyleMask.NSWindowStyleMaskMiniaturizable
            |MacBinding.NSWindowStyleMask.NSWindowStyleMaskResizable
            //|MacBinding.NSWindowStyleMask.NSWindowStyleMaskClosable
            );
        var surface = MacBinding.window_create_surface(window);
        //MacBinding.start_app(app);
        MacBinding.window_makeKeyAndOrderFront(window);

        while (true)
        {
            MacBinding.pump_messages(&message_loop,true);
        }
        
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    static unsafe void message_loop(InputEventStruct* input)
    {
        Console.WriteLine("Input: " + input->type.ToString());
    }
}
