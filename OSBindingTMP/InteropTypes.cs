using System.Runtime.InteropServices;

namespace OSBindingTMP;
[StructLayout(LayoutKind.Explicit)]
public struct InputEventStruct
{
    public const int KEYBOARD_EVENT = 1;
    public const int MOUSE_EVENT = 2;
    public const int WINDOW_EVENT = 4;
    [FieldOffset(0)] public int type;
    [FieldOffset(4)] public int internal_type;
    [FieldOffset(8)] public MouseEvent mouse;
    [FieldOffset(8)] public KeyboardEvent keyboard;
    [FieldOffset(8)] public WindowEvent window;
    
    [StructLayout(LayoutKind.Sequential)]

    public struct MouseEvent
    {
        public const int DOWN = 0;
        public const int HOLD = 1;
        public const int UP = 2;
        public const int MOVE = 3;
        public const int ENTER = 4;
        public const int EXIT = 5;
        
        public int local_x;
        public int local_y;
        public int global_x;
        public int global_y;
        public ulong button_state;
        public int button_action;
        public long button_refered;
        // public int state;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardEvent
    {
        public const int DOWN = 0;
        public const int UP = 2;

        public unsafe void* translated_key;
        public unsafe void* translated_unmodified_key;
        public ushort keycode;
        public uint flags;
        public int action;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowEvent
    {
        public long windowID;
        public int event_type;
        public unsafe void* data;

        public struct ResizeEvent
        {
            public int w, h;
        }
    }
}


[StructLayout(LayoutKind.Sequential)]
public struct NSApp
{
    public unsafe void* ptr;
}

[StructLayout(LayoutKind.Sequential)]
public struct NSWindow
{
    public unsafe void* ptr;
}