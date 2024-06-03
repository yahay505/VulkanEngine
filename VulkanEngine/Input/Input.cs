using System.Runtime.Intrinsics;
using VulkanEngine.ECS_internals.Resources;
using WindowsBindings;

namespace VulkanEngine.Input;

public static class Input
{
    static List<char> enteredThisFrame = new();
    // static IInputContext context;
    public static ECSResource InputResource = new ECSResource("INPUT");
    public static ulong down, up, pressed;
    private static ulong[] keyb = [0ul,0ul,0ul,0ul];

    // static IMouse mouse;
    public static float2 globalMousePos = default;
    public static float2 mouseDelta = default;
    public static float2 mouseScroll = default;

    #region internal

    

    public static void Init()
    {
        return;
    }

    public static void GetInput()
    {
        mouseDelta = default;
        switch (MIT.OS)
        {
            case OSType.Windows:
                GetInputWin();
                break;
            case OSType.Mac:
                throw new NotImplementedException();
                break;
            case OSType.Linux:
                throw new NotImplementedException();
                break;
            case OSType.Unknown:
                throw new NotImplementedException();
                break;
            case OSType.Android:
                throw new NotImplementedException();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static unsafe void GetInputWin()
    {
        var dpi = 4f;
        var data = WinAPI.pump_messages(false);
        up = data.up;
        down = data.down;
        pressed = data.pressed;
        if (data.mouseXRaw!=0||data.mouseYRaw!=0)
            Console.WriteLine($"{data.mouseXRaw}-{data.mouseYRaw}");
        if (data.isRawRelative)
        {
            mouseDelta = new float2 (data.mouseXRaw, -data.mouseYRaw)/dpi;
        }
        else
        {
            mouseDelta = new float2(data.mouseXRaw, -data.mouseYRaw)/dpi - mouseDelta;
        }

        globalMousePos = new(data.mouseXSoft, data.mouseYSoft);

        mouseScroll = new(data.scrollX, data.scrollY);
        for (int i = 0; i < data.scanCodeCount; i++)
        {
            var flag = data.scanCodesPtr[i * 2];
            var scanCode = data.scanCodesPtr[i * 2 + 1];
            if ((flag & 2)!=0)
            {
                scanCode = (ushort) (scanCode & 255 | 0xe000);
            }

            byte index = (byte) PlatformCodeConsolidator.OSToEnum(scanCode);
            
            var j = index % 64;
            var k = index / 64;
            var mask = 1ul << j;
            fixed (ulong* arr = &keyb[0])
            {
                arr[k] |= mask;
                mask = (flag & 1ul) << j; //if keyup   
                arr[k] &= ~(mask);
            }
            Console.Write($"{(mask == 0 ? """U""" : """D""")} {(Keys)index} - ");
        }
        // Console.WriteLine($"up:{up},down:{down},pressed:{pressed}, delta:{mouseDelta}, keyb:{keyb[0]}-{keyb[1]}-{keyb[2]}-{keyb[3]}");
    }

    #endregion

    #region api

    


        public static bool MouseButton(int i)
        {
            return (pressed & (1ul << i)) != 0;
        }

        public static bool Key(Keys key)
        {
            unsafe
            {
                byte index = (byte) key;
                var j = index % 64;
                var k = index / 64;
                return (keyb[k] & (1ul << j)) != 0;
            }
        }

        #endregion

}

// [Flags]
// public enum MouseButtonStates:byte
// {
//     Down=1,
//     Pressed=2,
//     Released=4
// }