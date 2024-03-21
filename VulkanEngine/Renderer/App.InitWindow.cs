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
            Title = "🎉🤯🎉",
            VSync = true,
            TransparentFramebuffer = true,
            // WindowBorder = WindowBorder.Hidden,
            
            
        } ;
   
        window = Window.Create(options);
        window.Initialize();

        
        
        
        Console.WriteLine();
        
        
        
        if (window.VkSurface is null)
        {
            throw new Exception("Windowing platform doesn't support Vulkan.");
        }
    }
}