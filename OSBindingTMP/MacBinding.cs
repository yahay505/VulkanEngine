using System.Runtime.InteropServices;

namespace OSBindingTMP;

public class MacBinding
{
 public const string Library = "MacFFI/out/a";
 private static readonly string[] mac_windowing_instance_requirements = 
  { "VK_KHR_surface", "VK_EXT_metal_surface"};
 public static Span<string> GetInstanceRequirements()
 {
  return new Span<string>(mac_windowing_instance_requirements);
 }

    [DllImport(Library)]
    public static extern bool install_global_listener();
    
    [DllImport(Library)]
    public static extern void main();
    
    [DllImport(Library)]
    public static extern unsafe void pump_messages(delegate* unmanaged[Cdecl]<InputEventStruct*,void> callback, bool wait_for_messages);
    
    [DllImport(Library)]
    public static extern unsafe NSApp create_application();

    [DllImport(Library)]
    public static extern void start_app(NSApp app);

    [DllImport(Library)]
    public static extern void window_makeKeyAndOrderFront(NSWindow window);
    
    [DllImport(Library, CharSet = CharSet.Ansi)]
    public static extern NSWindow open_window(string title, int width, int height, int x, int y, NSWindowStyleMask style);
    
    [DllImport(Library)]
    public static extern nint window_create_surface(NSWindow window);
    
    [Flags]
    public enum NSWindowStyleMask: uint {
        NSWindowStyleMaskBorderless = 0,
        NSWindowStyleMaskTitled = 1 << 0,
        NSWindowStyleMaskClosable = 1 << 1,
        NSWindowStyleMaskMiniaturizable = 1 << 2,
        NSWindowStyleMaskResizable	= 1 << 3,
    
        /* Specifies a window with textured background. Textured windows generally don't draw a top border line under the titlebar/toolbar. To get that line, use the NSUnifiedTitleAndToolbarWindowMask mask.
         */
        //API_DEPRECATED("Textured window style should no longer be used", macos(10.2, 11.0))
        NSWindowStyleMaskTexturedBackground  = 1 << 8,
    
        /* Specifies a window whose titlebar and toolbar have a unified look - that is, a continuous background. Under the titlebar and toolbar a horizontal separator line will appear.
         */
        NSWindowStyleMaskUnifiedTitleAndToolbar = 1 << 12,
    
        /* When present, the window will appear full screen. This mask is automatically toggled when toggleFullScreen: is called.
         */
        //API_AVAILABLE(macos(10.7)) 
        NSWindowStyleMaskFullScreen = 1 << 14,
    
        /* If set, the contentView will consume the full size of the window; it can be combined with other window style masks, but is only respected for windows with a titlebar.
         Utilizing this mask opts-in to layer-backing. Utilize the contentLayoutRect or auto-layout contentLayoutGuide to layout views underneath the titlebar/toolbar area.
         */
        //API_AVAILABLE(macos(10.10))
        NSWindowStyleMaskFullSizeContentView = 1 << 15,
    
        /* The following are only applicable for NSPanel (or a subclass thereof)
         */
        NSWindowStyleMaskUtilityWindow			= 1 << 4,
        NSWindowStyleMaskDocModalWindow 		= 1 << 6,
        NSWindowStyleMaskNonactivatingPanel		= 1 << 7, // Specifies that a panel that does not activate the owning application
        
        //API_AVAILABLE(macos(10.6)) 
        NSWindowStyleMaskHUDWindow = 1 << 13 // Specifies a heads up display panel
    };

}