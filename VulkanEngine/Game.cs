using System.Diagnostics;
using System.Numerics;
using ImGuiNET;

namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    static Camera camera = new();
    static FPSCounter fpsCounter = new(2000);
    public static void Run()
    {
#if !MAC
        CompileShadersWindowsTEMP();
#endif
        VKRender.InitializeRenderer();
        Start();
        while (!VKRender.window!.IsClosing)
        {
            VKRender.window.DoEvents();
            VKRender.UpdateTime();
            var fps=fpsCounter.AddAndGetFrame();
            VKRender.imGuiController.Update(VKRender.deltaTime);
            DisplayFps();
            ImGui.ShowDemoWindow();
            VKRender.Render();
            //input
            //systems
            Update();
            //physics
            // DrawFrame();
        }
        VKRender.vk.DeviceWaitIdle(VKRender.device);
        VKRender.CleanUp();
    }

    private static void CompileShadersWindowsTEMP()
    {
        //if env has renderdoc return early
        if (Environment.GetEnvironmentVariable("RENDERDOCeee") != null)
        {
            return;
        }
        //glslc
        ///Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.vert -o vert.spv
         //   /Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.frag -o frag.spv
         var processStartInfo = new ProcessStartInfo
             (){
                 UseShellExecute = false,
                 FileName = "glslc",
                 Arguments = "triangle.vert -o vert.spv",
                 //wd = cwd/../../
                 WorkingDirectory = System.IO.Directory.GetCurrentDirectory()+"/../../../",
             };
         Process.Start(processStartInfo)!.WaitForExit();
         processStartInfo.Arguments = "triangle.frag -o frag.spv";
         Process.Start(processStartInfo )!.WaitForExit();
    }

    public static void Update()
    {
        var cam_dist=3f;
        // Thread.Sleep(15);
        // camera.transform.
    }

    public static void Start()
    {
        
    }

    private static void DisplayFps()
    {
        int location = 0;
        var io = ImGui.GetIO();
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysAutoResize |
                    ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoSavedSettings;

        if (location >= 0)
        {
            const float PAD = 10.0f;
            var viewport = ImGui.GetMainViewport();
            var work_pos = viewport.WorkPos; // Use work area to avoid menu-bar/task-bar, if any!
            var work_size = viewport.WorkSize;
            Vector2 window_pos, window_pos_pivot;
            window_pos.X = (location & 1) != 0 ? (work_pos.X + work_size.X - PAD) : (work_pos.X + PAD);
            window_pos.Y = (location & 2) != 0 ? (work_pos.Y + work_size.Y - PAD) : (work_pos.Y + PAD);
            window_pos_pivot.X = (location & 1) != 0 ? 1.0f : 0.0f;
            window_pos_pivot.Y = (location & 2) != 0 ? 1.0f : 0.0f;
            
            ImGui.SetNextWindowPos(window_pos, ImGuiCond.Always, window_pos_pivot);
            flags |= ImGuiWindowFlags.NoMove;
        } else if (location == -2)
        {
            ImGui.SetNextWindowPos(io.DisplaySize - new Vector2(0.0f, 0.0f), ImGuiCond.Always, new Vector2(1.0f, 1.0f));
            flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        }
        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Example: Simple overlay", ImGuiWindowFlags.NoDecoration |
                                                    ImGuiWindowFlags.AlwaysAutoResize |
                                                    ImGuiWindowFlags.NoSavedSettings |
                                                    ImGuiWindowFlags.NoFocusOnAppearing |
                                                    ImGuiWindowFlags.NoNav))
        {
            // TODO: Rightclick to change pos
            ImGui.Text("Demo Scene");
            ImGui.Separator();
            ImGui.Text($"Application average {1000.0f / ImGui.GetIO().Framerate:F3} ms/frame ({ImGui.GetIO().Framerate:F1} FPS)");
            ImGui.End();
        }
        
    }
}