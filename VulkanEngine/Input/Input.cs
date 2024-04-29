using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.Input;

public static class Input
{
    static List<char> enteredThisFrame = new();
    // static IInputContext context;
    public static ECSResource InputResource = new ECSResource("INPUT");
    
    static List<KeyState> KeyStates = new();
    static List<KeyState> MouseKeyStates = new();
    // static IMouse mouse;
    public static float2 lastMousePosition = new(0,0);
    public static float2 mousePosition = new(0,0);
    public static float2 mouseDelta = new(0,0);
    public static float2 lastMouseScroll = new(0,0);
    public static float2 mouseScroll = new(0,0);
    public static float2 mouseScrollDelta = new(0,0);
    
    public static void Init()
    {
        return;
        // context = _context;//bad code
        // context.Keyboards.ForEach((a)=>
        // {
            // a.SupportedKeys.ForEach((b)=>
            // {
                // if ((int)b>=KeyStates.Count)
                // {
                    // KeyStates.AddRange(new KeyState[(int) ((b)-KeyStates.Count+1)]);
                // }
                // KeyStates[(int)b]=0;
            // });
            // a.KeyDown += OnKeyDown;
            // a.KeyUp += OnKeyUp;
            // a.KeyChar += OnChar;
        // });
       // context.Mice.ForEach(m =>
       // {
           // m.Click += OnMouseClick;
           // m.MouseMove += OnMouseMove;
           // m.Scroll += OnMouseScroll;
           // m.MouseDown += OnMouseDown;
           // m.MouseUp += OnMouseUp;
           // m.DoubleClick += OnMouseDoubleClick;   
       // });
       // MouseKeyStates.AddRange(new KeyState[20]);
    }

    // private static void OnMouseDoubleClick(IMouse arg1, MouseButton arg2, Vector2 arg3)
    // {
    //     throw new NotImplementedException();
    // }

    // private static void OnMouseUp(IMouse arg1, MouseButton arg2)
    // {
    //     MouseKeyStates[(int) arg2]&=~KeyState.Down;
    //     MouseKeyStates[(int) arg2]|=KeyState.Released;
    // }
    //
    // private static void OnMouseDown(IMouse arg1, MouseButton arg2)
    // {
    //     MouseKeyStates[(int) arg2]|=KeyState.Down|KeyState.Pressed;
    // }
    //
    // private static void OnMouseScroll(IMouse arg1, ScrollWheel arg2)
    // {
    //     mouseScroll = new(arg2.X,arg2.Y);
    //     mouseScrollDelta = mouseScroll - lastMouseScroll;
    // }
    //
    // private static void OnMouseMove(IMouse arg1, Vector2 arg2)
    // {
    //     mousePosition = new(arg2.X,arg2.Y);
    //     mouseDelta = mousePosition - lastMousePosition;
    // }
    //
    // private static void OnMouseClick(IMouse arg1, MouseButton arg2, Vector2 arg3)
    // {
    //     // throw new NotImplementedException();
    // }

    public static void Update()
    {
        ClearFrameState();
        lastMousePosition = mousePosition;
        lastMouseScroll = mouseScroll;

        // VKRender.mainWindow.window.DoEvents();
    }

    private static void ClearFrameState()
    {
        KeyStates.ForEachRef((ref KeyState a) => { a &= ~(KeyState.Pressed | KeyState.Released); });
        MouseKeyStates.ForEachRef((ref KeyState a) => { a &= ~(KeyState.Pressed | KeyState.Released); });
        mouseDelta = new(0);
        mouseScrollDelta = new(0);
    }
    // private static void OnKeyDown(IKeyboard sender, Key key, int i)
    // {
    //     Console.Write("""🔽""");
    //     Console.WriteLine(key);
    //     KeyStates[(int) key]|=KeyState.Down|KeyState.Pressed;
    // }
    // private static void OnKeyUp(IKeyboard sender, Key key, int i)
    // {
    //     Console.Write("\ud83d\udd3c");
    //     Console.WriteLine(key);
    //     KeyStates[(int) key]&=~KeyState.Down;
    //     KeyStates[(int) key]|=KeyState.Released;
    // }
    // private static void OnChar(IKeyboard sender, char key)
    // {
    //     Console.Write("""⌨""");
    //     Console.WriteLine(key);
    // }
    
    // public static bool Key(Key key)
    // {
    //     return (KeyStates[(int) key] & KeyState.Down) != 0;
    // }
    // public static bool KeyPressed(Key key)
    // {
    //     return (KeyStates[(int) key] & KeyState.Pressed) != 0;
    // }
    // public static bool KeyReleased(Key key)
    // {
    //     return (KeyStates[(int) key] & KeyState.Released) != 0;
    // }
    // public static bool MouseButton(MouseButton button)
    // {
    //     return (MouseKeyStates[(int) button] & KeyState.Down)!=0;
    // }
    // public static bool MouseButtonPressed(MouseButton button)
    // {
    //     return (MouseKeyStates[(int) button] & KeyState.Pressed)!=0;
    // }
    //
    // public static bool MouseButtonReleased(MouseButton button)
    // {
    //     return (MouseKeyStates[(int) button] & KeyState.Released) != 0;
    // }

}
[Flags]
public enum KeyState:byte
{
    Down=1,
    Pressed=2,
    Released=4
}
// [Flags]
// public enum MouseButtonStates:byte
// {
//     Down=1,
//     Pressed=2,
//     Released=4
// }