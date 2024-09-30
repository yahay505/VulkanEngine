using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OSBindingTMP;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;
using WindowsBindings;

namespace VulkanEngine.Phases.FramePreamblePhase;

public static class PreambleSequence
{
    public static void Register()
    {
        var HandleInputWindow = new ExecutionUnitBuilder(HandleInputAndWindow)
            .Named("HandleInputnWindow")
            // .Writes(VKRender.RendererEcsResource,Input.Input.InputResource) // make more granular
            .Build();
        ScheduleMaker.RegisterToTarget(HandleInputWindow, "frame_preamble");
    }
    private static void HandleInputAndWindow()
    {
        Input.Input.GetInput();

        if (
            //VKRender.mainWindow.window!.IsClosing||
            Volatile.Read(ref Scheduler.Stopping)
            )
        {
            Scheduler.Stop();
        }
        //Input.Input.Update();
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    static unsafe void callback(InputEventStruct* inevent)
    {
        Console.WriteLine(inevent->type);
    }
    static unsafe void win_callback(InputStruct input)
    {
        // Console.WriteLine(input);
    }
}