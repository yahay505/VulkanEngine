using System.Diagnostics;
using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Vulkan.Extensions.ImGui;
using VulkanEngine.ECS_internals;
using VulkanEngine.Renderer;
using VulkanEngine.Renderer.ECS;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine;

public static class EngineStart
{
    public static void StartEngine()
    {
        foreach (var type in System.Reflection.Assembly.GetCallingAssembly().GetTypes())
        {
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        }
        
        MIT.Start();
        
        // Scheduler.Run(LoadTestLoop());
        
        CompileShadersTEMP();
        // VKRender.InitVulkanSecondPhase();
        // VKRender.InitWindow();1
        VKRender.mainWindow = VKRender.CreateWindow(new(800, 600),"VulkanEngine");
        VKRender.CreateSwapchain(VKRender.mainWindow,true);
        VKRender.LoadMesh(VKRender.AssetsPath+"/models/model.obj");
        
        VKRender.InitVulkan();
        
        var InputCntx = VKRender.mainWindow.window.CreateInput();

        VKRender.imGuiController = new ImGuiController(VKRender.mainWindow.window,InputCntx,new ImGuiFontConfig(VKRender.AssetsPath+"/fonts/FiraSansCondensed-ExtraLight.otf",12),VKRender.physicalDevice,VKRender._familyIndices.graphicsFamily!.Value,VKRender.mainWindow.SwapChainImages.Length,VKRender.mainWindow.swapChainImageFormat,VKRender.mainWindow.depthImage.ImageFormat,VKRender.device);
        ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        
        Input.Input.Init(InputCntx);

        ECS.RegisterSystem(MeshComponent._data,typeof(MeshComponent));

        Game.Run();
           
        vkDeviceWaitIdle(VKRender.device);
        VKRender.CleanUp();
    }
    private static void CompileShadersTEMP()
    {
        //if env has renderdoc return early
        if (Program.args.Any(a=>a=="skip_shader_compilation")||Environment.GetEnvironmentVariable("skip_shader_compilation") != null)
        {
            return;
        }
        //glslc
        ///Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.vert -o vert.spv
        //   /Users/yavuz/VulkanSDK/1.3.261.1/macOS/bin/glslc triangle.frag -o frag.spv

        var www=new[]
            {
                "*.vert",
                "*.frag",
                "*.comp",
            }.SelectMany(search_string => Directory.GetFiles(VKRender.AssetsPath + "/shaders",
                search_string,
                SearchOption.AllDirectories))
            .Select(in_name =>
           {
                var out_name = VKRender.AssetsPath + "/shaders/compiled" +
                               in_name[((VKRender.AssetsPath + "/shaders").Length)..] + ".spv";

                return Process.Start("glslc", $@"{in_name} -o {out_name}");
            }).ToArray();
        // wait for all in parallel
        foreach (var process in www)
        {
            process.WaitForExit();
        }

    }



}