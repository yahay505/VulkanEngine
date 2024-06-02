using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;
using OSBindingTMP;
using Pastel;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;

namespace VulkanEngine.Phases.FramePreamblePhase;

public static class PreambleSequence
{
    public static void Register()
    {
        var HandleInputWindow = new ExecutionUnitBuilder(HandleInputAndWindow)
            .Named("HandleInputnWindow")
            .Writes(VKRender.RendererEcsResource,Input.Input.InputResource) // make more granular
            .Build();
        ScheduleMaker.RegisterToTarget(HandleInputWindow, "frame_preamble");
    }
    private static void HandleInputAndWindow()
    {
        Input.Input.ProcessNextFrameEvents();
        if (
            //VKRender.mainWindow.window!.IsClosing||
            Volatile.Read(ref Scheduler.Stopping)
            )
        {
            Scheduler.Stop();
        }
        //Input.Input.Update();
    }
    // private static bool is_in_window = false;
    // [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    // static unsafe void callback(InputEventStruct* inevent)
    // {
    //     if (inevent->type == InputEventStruct.MOUSE_EVENT)
    //     {
    //         if (inevent->mouse.button_action == 4) is_in_window = true;
    //         if (inevent->mouse.button_action == 5) is_in_window = false;
    //     }
    //     Console.Write($"{inevent->type}:[{inevent->internal_type:D2}] ".Pastel(is_in_window?ConsoleColor.Green:ConsoleColor.Red));   
    //     if (inevent->type == InputEventStruct.MOUSE_EVENT)
    //         Console.WriteLine($"mouse x:{inevent->mouse.local_x:D4} y:{inevent->mouse.local_y:D4} button_ref:{inevent->mouse.button_refered:D2} b_act:{inevent->mouse.button_action:D1} global x:{inevent->mouse.global_x:D4} y:{inevent->mouse.global_y:D4} button_state:{inevent->mouse.button_state:B64}");
    //     else if (inevent->type == InputEventStruct.KEYBOARD_EVENT)
    //         Console.WriteLine($"keyboard {(inevent->keyboard.action == 0 ? "🔽" : "🔼")} {inevent->keyboard.keycode} chars:{Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)inevent->keyboard.translated_key))} chars_unmod:{Encoding.UTF8.GetString(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)inevent->keyboard.translated_unmodified_key))} flags:{inevent->keyboard.flags:B32}");
    //     else
    //         Console.WriteLine("non mouse event");
    // }
}
