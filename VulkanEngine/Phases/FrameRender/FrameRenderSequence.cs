using System.Diagnostics;
using System.Numerics;
// using ImGuiNET;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;
using VulkanEngine.Renderer.ECS;

namespace VulkanEngine.Phases.FrameRender;

public class FrameRenderSequence
{
    public static void Register()
    {
        var writeoutRenderObjectsUnit = new ExecutionUnitBuilder(WriteoutRenderObjects.WriteOutObjectData)
            .Named("WriteoutRenderObjects")
            .Reads(TransformSystem.Resource,MeshComponent.Resource)
            .Writes(VKRender.RendererEcsResource)
            .Build();
        var renderUnit = new ExecutionUnitBuilder(Render)
            .RunsAfter(writeoutRenderObjectsUnit)
            .Named("Render")
            .Reads(TransformSystem.Resource)
            .Writes(VKRender.RendererEcsResource,VKRender.IMGUIResource)
            .Build();
        ScheduleMaker.RegisterToTarget(writeoutRenderObjectsUnit, "frame_render");
        ScheduleMaker.RegisterToTarget(renderUnit, "frame_render");
    }
    private static void Render()
    {
        VKRender.UpdateTime();
        DisplayFps();
        unsafe
        {
            var query = MakeQuery<Transform_ref,Camera_ref>();
            if (!HasResults(ref query, out _, out var transform, out var camera))
            {
                throw new NotImplementedException();
            }
            VKRender.SetCamera(transform, camera.data,VKRender.mainWindow.size.ToInt2());
        }

        //Editor.EditorRoot.Render();            

        VKRender.Render();
        //VKRender.imGuiController.Update(VKRender.deltaTime);

    }
    private static void DisplayFps()
    {
        // int location = 0;
        // var io = ImGui.GetIO();
        // var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize |
        //             ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;
        //
        // if (location >= 0)
        // {
        //     const float PAD = 10.0f;
        //     var viewport = ImGui.GetMainViewport();
        //     var work_pos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
        //     var work_size = viewport.WorkSize;
        //     Vector2 window_pos, window_pos_pivot;
        //     window_pos.X = (location & 1) != 0 ? (work_pos.X + work_size.X - PAD) : (work_pos.X + PAD);
        //     window_pos.Y = (location & 2) != 0 ? (work_pos.Y + work_size.Y - PAD) : (work_pos.Y + PAD);
        //     window_pos_pivot.X = (location & 1) != 0 ? 1.0f : 0.0f;
        //     window_pos_pivot.Y = (location & 2) != 0 ? 1.0f : 0.0f;
        //     
        //     ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
        //     flags |= ImGuiWindowFlags.NoMove;
        // } else if (location == -2)
        // {
        //     ImGui.SetNextWindowPos(io.DisplaySize - new Vector2(0.0f, 0.0f), ImGuiCond.Always, new Vector2(1.0f, 1.0f));
        //     flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        // }
        // ImGui.SetNextWindowBgAlpha(0.35f);
        // if (ImGui.Begin("Example: Simple overlay", ImGuiWindowFlags.NoDecoration |
        //                                             ImGuiWindowFlags.AlwaysAutoResize |
        //                                             ImGuiWindowFlags.NoSavedSettings |
        //                                             ImGuiWindowFlags.NoFocusOnAppearing |
        //                                             ImGuiWindowFlags.NoNav))
        // {
        //     // TODO: Rightclick to change pos
        //     ImGui.Text("Demo Scene");
        //     ImGui.Separator();
        //     ImGui.Text($"Frame# {VKRender.CurrentFrame}");
        //     ImGui.Text($"Application average {1000.0f / ImGui.GetIO().Framerate:F3} ms/frame ({ImGui.GetIO().Framerate:F1} FPS)");
        //     ImGui.Text($"Time spent calculating(not rendering): {(Stopwatch.GetTimestamp()-Volatile.Read(ref Game.calcMS))/(float)Stopwatch.Frequency * 1000f:F4} ms");
        //     ImGui.End();
        // }
        //
    }
}