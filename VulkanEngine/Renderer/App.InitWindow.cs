using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private static void InitWindow()
    {
        //Create a window.
        var options = WindowOptions.DefaultVulkan with
        {
            Size = new Vector2D<int>(Width, Height),
            Title = "Vulkan2",
        };

        window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }
}