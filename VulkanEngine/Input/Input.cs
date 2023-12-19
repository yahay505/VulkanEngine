using Silk.NET.Input;

namespace VulkanEngine.Input;

public static class Input
{
    static List<char> enteredThisFrame = new();
    static IInputContext context;
    static List<KeyState> KeyStates = new();
    public static void Init(IInputContext _context)
    {
        context = _context;//bad code
        context.Keyboards.ForEach((a)=>
        {
            a.SupportedKeys.ForEach((b)=>
            {
                if ((int)b>=KeyStates.Count)
                {
                    KeyStates.AddRange(new KeyState[(int) ((b)-KeyStates.Count+1)]);
                }
                KeyStates[(int)b]=0;
            });
            a.KeyDown += OnKeyDown;
            a.KeyUp += OnKeyUp;
            a.KeyChar += OnChar;
        });
    }
    public static void ClearFrameState()
    {
        KeyStates.ForEachRef((ref KeyState a) => { a &= ~(KeyState.Pressed | KeyState.Released); });
    }
    private static void OnKeyDown(IKeyboard sender, Key key, int i)
    {
        Console.WriteLine(key);
        KeyStates[(int) key]|=KeyState.Down|KeyState.Pressed;
    }
    private static void OnKeyUp(IKeyboard sender, Key key, int i)
    {
        Console.WriteLine(key);
        KeyStates[(int) key]&=~KeyState.Down;
        KeyStates[(int) key]|=KeyState.Released;
    }
    private static void OnChar(IKeyboard sender, char key)
    {
        Console.WriteLine(key);
    }
    
    public static bool Key(Key key)
    {
        return (KeyStates[(int) key] & KeyState.Down) != 0;
    }
    public static bool KeyPressed(Key key)
    {
        return (KeyStates[(int) key] & KeyState.Pressed) != 0;
    }
    public static bool KeyReleased(Key key)
    {
        return (KeyStates[(int) key] & KeyState.Released) != 0;
    }
    
}
[Flags]
internal enum KeyState:byte
{
    Down=1,
    Pressed=2,
    Released=4
}
