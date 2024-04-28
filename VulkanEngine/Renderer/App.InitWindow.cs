namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    // private static void InitWindow()
    // {
    //     //Create a window.
    //     var options = WindowOptions.DefaultVulkan with
    //     {
    //         size = new Vector2D<int>(Width, Height),
    //         Title = "🎉🤯🎉",
    //         VSync = true,
    //         TransparentFramebuffer = true,
    //         // WindowBorder = WindowBorder.Hidden,
    //     } ;
    //
    //     Window.PrioritizeGlfw();
    //     window = Window.Create(options);
    //     window.Initialize();
    //
    //     if (!window.TransparentFramebuffer)
    //     {
    //         throw new Exception("Windowing platform doesn't support transparent.");
    //     }
    //
    //     Console.WriteLine($"window initialized with as {window.GetType()}".Pastel(ConsoleColor.Green));
    //     
    //     if (window.VkSurface is null)
    //     {
    //         throw new Exception("Windowing platform doesn't support Vulkan.");
    //     }
    // }
}