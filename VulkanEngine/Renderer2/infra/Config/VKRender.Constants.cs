namespace VulkanEngine.Renderer2.infra;

public static partial class Infra
{
    private static readonly bool EnableValidationLayers =
#if DEBUG
        true;
#else
        false;
#endif

    internal const int FRAME_OVERLAP = 2;
}