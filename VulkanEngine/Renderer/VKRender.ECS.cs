using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static ECSResource RendererEcsResource = new ECSResource("Renderer");
    public static ECSResource IMGUIResource = new("IMGUI");

}
