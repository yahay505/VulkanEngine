using System.Runtime.InteropServices;

namespace OSBindingTMP;
[StructLayout(LayoutKind.Sequential)]
public struct InputEventStruct
{
    public int type;
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