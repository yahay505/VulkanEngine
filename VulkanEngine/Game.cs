using System.Diagnostics;
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
            ImGui.Text($"{VKRender.deltaTime*1000,6:F2}ms | {fps,4}fps");
            ImGui.ShowDemoWindow();
            // Console.WriteLine($"{VKRender.deltaTime*1000,6:F2}ms | {fps,4}fps");
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
}