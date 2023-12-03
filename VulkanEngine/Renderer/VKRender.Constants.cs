namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private static readonly bool EnableValidationLayers =
#if DEBUG
        true;
#else
        false;
#endif

    private const int FRAME_OVERLAP = 3;
}