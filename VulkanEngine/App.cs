using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
namespace VulkanEngine;

public static partial class App
{
    private const int Width=800;
    private const int Height=600;

    private static bool EnableValidationLayers = true;


    private static readonly string[] validationLayers = new[]
    {
        "VK_LAYER_KHRONOS_validation"
    };
    private static readonly string[] deviceExtensions = new[]
    {
        KhrSwapchain.ExtensionName
    }
#if true//mac
            .Append("VK_KHR_portability_subset").ToArray()
#endif
        ;
    
    private static IWindow? window;
    private static Vk? vk;

    private static Instance instance;

    private static ExtDebugUtils? debugUtils;
    private static DebugUtilsMessengerEXT debugMessenger;


    private static Queue graphicsQueue;
    private static Queue presentQueue;
    
    private static KhrSurface? khrSurface;
    private static SurfaceKHR surface;
    private static KhrSwapchain? khrSwapChain;
    private static SwapchainKHR swapChain;
    private static Image[]? swapChainImages;
    private static ImageView[]? swapChainImageViews;
    private static Format swapChainImageFormat;
    private static Extent2D swapChainExtent;
    private static Framebuffer[]? swapChainFramebuffers;
    
    public static RenderPass RenderPass;
    private static CommandPool CommandPool;
    private static CommandBuffer[] CommandBuffers;
    
    const int MAX_FRAMES_IN_FLIGHT = 2;
    static int CurrentFrame = 0;
    static int CurrentFrameIndex = 0;
    public static void Run()
    {
            InitWindow();
            InitVulkan();
            MainLoop();
            CleanUp();
    }

    private static void MainLoop()
    {
        while (!window!.IsClosing)
        {
            window!.DoEvents();
            DrawFrame();
            CurrentFrame++;
            CurrentFrameIndex =CurrentFrame% MAX_FRAMES_IN_FLIGHT;
        }
        vk!.DeviceWaitIdle(logicalDevice);
    }

    private static unsafe void CleanUp()
    {
        vk!.DestroyCommandPool(logicalDevice, CommandPool, null);
        
        foreach (var semaphore in imageAvailableSemaphores)
            vk!.DestroySemaphore(logicalDevice, semaphore, null);
        foreach (var semaphore in renderFinishedSemaphores)
            vk!.DestroySemaphore(logicalDevice, semaphore, null);
        foreach (var fence in inFlightFences)
            vk!.DestroyFence(logicalDevice, fence, null);
        foreach (var framebuffer in swapChainFramebuffers!)
            vk!.DestroyFramebuffer(logicalDevice, framebuffer, null);
        
        vk!.DestroyPipelineLayout(logicalDevice,PipelineLayout, null);
        vk!.DestroyPipeline(logicalDevice, GraphicsPipeline, null);
        vk!.DestroyRenderPass(logicalDevice, RenderPass, null);
        khrSwapChain!.DestroySwapchain(logicalDevice, swapChain, null);
        
for (var i = 0; i < swapChainImageViews?.Length; i++)
        {
            vk!.DestroyImageView(logicalDevice,swapChainImageViews[i],null);
        }
        vk!.DestroyDevice(logicalDevice, null);
        
        if (EnableValidationLayers)
        {
            //DestroyDebugUtilsMessenger equivalent to method DestroyDebugUtilsMessengerEXT from original tutorial.
            debugUtils!.DestroyDebugUtilsMessenger(instance, debugMessenger, null);
        }
        khrSurface!.DestroySurface(instance, surface, null);
        
        vk!.DestroyInstance(instance, null);
        vk!.Dispose();

        window?.Dispose();
        
    }
}