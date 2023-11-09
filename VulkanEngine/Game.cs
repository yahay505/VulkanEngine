namespace VulkanEngine;
using VulkanEngine.Renderer;

public static class Game
{
    static Camera camera = new();
    public static void Run()
    {
        VKRender.InitializeRenderer();
        Start();
        while (!VKRender.window!.IsClosing)
        {
            VKRender.window.DoEvents();
            VKRender.Render();
            //input
            //systems
            Update();
            //physics
            VKRender.Update();
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