using System.Diagnostics;
using ImGuiNET;

namespace VulkanEngine.Editor;

public static class EditorRoot
{
    [Conditional("DEBUG")]
    public static void Render()
    {
        ImGui.Begin("Editor");
        
        ImGui.End();
    }
}