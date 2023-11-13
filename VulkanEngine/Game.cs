using ImGuiNET;

namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    static Camera camera = new();
    static FPSCounter fpsCounter = new(2000);
    public static void Run()
    {
        
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

    public static void Update()
    {
        var cam_dist=3f;
        
        // camera.transform.
    }

    public static void Start()
    {
        
    }
}