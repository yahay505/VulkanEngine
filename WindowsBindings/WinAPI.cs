using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WindowsBindings;

public static class WinAPI
{
    private static ushort registerClass;
    private static PCWSTR lpWindowName;
    private static HMODULE hinstance;
    private static bool FUNCKİNG_BREAK= false;
    public static int lenght = 0;
    private static unsafe ushort* scanCodeBuf = (ushort*) NativeMemory.Alloc(400);
    static unsafe LRESULT WinMain(HWND WindowHandle, uint eventID,
            WPARAM wparam, LPARAM lparam)
        {
            // Console.WriteLine($"forwarded to window procedure: 0x{eventID:x}: {(WIN_EVENTS) eventID}");
            switch ((WIN_EVENTS) eventID)
            {
                    case (WIN_EVENTS)0x0010:
                    {
                        throw new Exception("window closed");
                    }
                case WIN_EVENTS.WM_PAINT:
                    FUNCKİNG_BREAK = true;
                    goto default;
                    break;
                case (WIN_EVENTS) 0x0231:
                    Console.WriteLine("window:WM_APP");
                    return (LRESULT) 0;
                    goto default;
                case WIN_EVENTS.WM_NCHITTEST:
                    // Console.WriteLine("window:WM_NCHITTEST");
                    // return (LRESULT) 0;
                    goto default;
                case WIN_EVENTS.WM_INPUT:
                    var risize = 0u;
                    PInvoke.GetRawInputData((HRAWINPUT) lparam.Value, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, null,
                        &risize,
                        (uint) Marshal.SizeOf<RAWINPUTHEADER>()).CheckIsZero();
                    var b = stackalloc byte[(int) risize];
                    var rawInput = (RAWINPUT*) b;
                    PInvoke.GetRawInputData((HRAWINPUT) lparam.Value, RAW_INPUT_DATA_COMMAND_FLAGS.RID_INPUT, rawInput,
                        &risize, (uint) Marshal.SizeOf<RAWINPUTHEADER>()).CheckNotNegativeOne();

                    PInvoke.GetRawInputData((HRAWINPUT) lparam.Value, RAW_INPUT_DATA_COMMAND_FLAGS.RID_HEADER, null,
                        &risize,
                        (uint) Marshal.SizeOf<RAWINPUTHEADER>()).CheckNotNegativeOne();
                    var a = stackalloc byte[(int) risize];
                    RAWINPUTHEADER* rawinputheader = (RAWINPUTHEADER*) a;
                    PInvoke.GetRawInputData((HRAWINPUT) lparam.Value, RAW_INPUT_DATA_COMMAND_FLAGS.RID_HEADER,
                        rawinputheader, &risize, (uint) Marshal.SizeOf<RAWINPUTHEADER>()).CheckNotNegativeOne();

                    switch (rawinputheader->dwType)
                    {
                        case 0:
                            // Console.WriteLine("WM_INPUT: Mouse");
                            var mouse = rawInput->data.mouse;
                            var flags = (MOUSE_INPUT_CONSTANTS) mouse.Anonymous.Anonymous.usButtonFlags;
                            var scroll_data = mouse.Anonymous.Anonymous.usButtonData;
                            var is_xy_relative = (flags & MOUSE_INPUT_CONSTANTS.MOUSE_MOVE_ABSOLUTE) == 0;
                            var x = mouse.lLastX;
                            var y = mouse.lLastY;
                            var do_we_have_yscroll = ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_WHEEL) != 0);
                            var do_we_have_xscroll = ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_HWHEEL) != 0);
                            long down, up;
                            down =
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_1_DOWN) != 0 ? 1 : 0) << 0 |
                                (((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_2_DOWN) != 0) ? 1 : 0) << 1 |
                                (((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_3_DOWN) != 0) ? 1 : 0) << 2 |
                                (((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_4_DOWN) != 0) ? 1 : 0) << 3 |
                                (((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_5_DOWN) != 0) ? 1 : 0) << 4;
                            up =
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_1_UP) != 0 ? 1 : 0) << 0 |
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_2_UP) != 0 ? 1 : 0) << 1 |
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_3_UP) != 0 ? 1 : 0) << 2 |
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_4_UP) != 0 ? 1 : 0) << 3 |
                                ((flags & MOUSE_INPUT_CONSTANTS.MOUSE_BUTTON_5_UP) != 0 ? 1 : 0) << 4;
                            if (do_we_have_yscroll)
                            {
                                ret.scrollY += scroll_data;
                            }
                            else if (do_we_have_xscroll)
                            {
                                ret.scrollX += scroll_data;
                            }

                            ret.down |= (ulong) down;
                            ret.up |= (ulong) up;
                            ret.pressed = (ret.pressed & (ulong) ~up) | (ulong) down;
                            ret.isRawRelative = is_xy_relative;
                            ret.mouseXRaw = (is_xy_relative ? ret.mouseXRaw : 0) + x;
                            ret.mouseYRaw = (is_xy_relative ? ret.mouseYRaw : 0) + y;

                            break;
                        case 1:
                            // Console.WriteLine("WM_INPUT: Keyboard");
                            var keyboard = rawInput->data.keyboard;
                            var scanCode = keyboard.MakeCode;
                            if (ret.scanCodeCount >= 100)
                            {
                                break;
                            }

                            scanCodeBuf[ret.scanCodeCount * 2] = keyboard.Flags;
                            scanCodeBuf[ret.scanCodeCount * 2 + 1] = scanCode;
                            ret.scanCodeCount++;

                            break;
                        case 2:
                            // Console.WriteLine("WM_INPUT: HID");
                            break;
                    }

                    // Console.WriteLine("WM_INPUT");
                    return (LRESULT) 0;
                case WIN_EVENTS.WM_INPUT_DEVICE_CHANGE:
                // Console.WriteLine("WM_INPUT_DEVICE_CHANGE");
                // return (LRESULT) 0;
                default:
                    return PInvoke.DefWindowProc(WindowHandle, eventID, wparam, lparam);
            }
            
        }

    public static void create_app()
    {
        
        unsafe
        {
            Console.WriteLine("Hello, World!");
            //var messageBox = PInvoke.MessageBox((HWND)0, "Hello, World!", "Hello,", MESSAGEBOX_STYLE.MB_HELP);
            hinstance = PInvoke.GetModuleHandle((PCWSTR) null);

            fixed (char* name = "asas")
            {
                lpWindowName = new PCWSTR(name);
            }

            var lpWndClass = new WNDCLASSW()
            {
                lpfnWndProc = WinMain,
                hInstance = hinstance,
                lpszClassName = lpWindowName,
                style = WNDCLASS_STYLES.CS_HREDRAW| WNDCLASS_STYLES.CS_VREDRAW | WNDCLASS_STYLES.CS_OWNDC,
            };
            registerClass = PInvoke.RegisterClass(lpWndClass);
            Console.WriteLine(registerClass);
           



        }
    }

    

    public unsafe static nint open_window()
    {
         var window = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_LEFT /*| WINDOW_EX_STYLE.WS_EX_LAYERED*/, lpWindowName, lpWindowName,
                WINDOW_STYLE.WS_OVERLAPPEDWINDOW | WINDOW_STYLE.WS_VISIBLE, 0, 0, 300, 400, (HWND) 0, (HMENU) 0,
                hinstance);
         // PInvoke.SetPixelFormat(PInvoke.GetDC(window),)
         var rgn = PInvoke.CreateRectRgn(0,0,-1,-1);
         DWM_BLURBEHIND bb;
         bb.dwFlags = (0b11);
         bb.fEnable = true;
         bb.hRgnBlur = rgn;
         bb.fTransitionOnMaximized = false;
         PInvoke.DwmEnableBlurBehindWindow(window, &bb);
            PInvoke.ShowWindow(window, (SHOW_WINDOW_CMD.SW_NORMAL));
            // PInvoke.UpdateLayeredWindow(window,PInvoke.GetDC(default),null,null,default,null,new COLORREF(255), (BLENDFUNCTION?)(null),UPDATE_LAYERED_WINDOW_FLAGS.ULW_ALPHA);
            //register Raw Input
            var rids = stackalloc RAWINPUTDEVICE[]
            {
                new()
                {
                    usUsagePage = 1,
                    usUsage = 6,
                    dwFlags = RAWINPUTDEVICE_FLAGS.RIDEV_NOLEGACY|RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK|RAWINPUTDEVICE_FLAGS.RIDEV_DEVNOTIFY,
                    hwndTarget = window
                },
                new()
                {
                    usUsagePage = 1,
                    usUsage = 2,
                    dwFlags = 
                        // RAWINPUTDEVICE_FLAGS.RIDEV_NOLEGACY|
                        RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK|
                        RAWINPUTDEVICE_FLAGS.RIDEV_DEVNOTIFY,
                        
                    hwndTarget = window
                },
                new ()
                {
                    usUsagePage = 1,
                    usUsage = 4,
                    dwFlags = 
                        // RAWINPUTDEVICE_FLAGS.RIDEV_NOLEGACY|
                        RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK|
                        RAWINPUTDEVICE_FLAGS.RIDEV_DEVNOTIFY,
                    hwndTarget = window
                },
                new ()
                {
                    usUsagePage = 1,
                    usUsage = 5,
                    dwFlags = 
                        // RAWINPUTDEVICE_FLAGS.RIDEV_NOLEGACY|
                        RAWINPUTDEVICE_FLAGS.RIDEV_INPUTSINK|
                        RAWINPUTDEVICE_FLAGS.RIDEV_DEVNOTIFY,
                    hwndTarget = window
                }
                
            };
            
            if (!PInvoke.RegisterRawInputDevices(rids, 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                throw new (Marshal.GetLastPInvokeErrorMessage()+"1");
            }
            if (!PInvoke.RegisterRawInputDevices(&rids[1], 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                throw new (Marshal.GetLastPInvokeErrorMessage()+"2");
            }            
            if (!PInvoke.RegisterRawInputDevices(&rids[2], 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                throw new (Marshal.GetLastPInvokeErrorMessage()+"3");
            }
            if (!PInvoke.RegisterRawInputDevices(&rids[3], 1, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
            {
                throw new (Marshal.GetLastPInvokeErrorMessage()+"4");
            }
            // PInvoke.SetWindowLongPtr(window, WINDOW_LONG_PTR_INDEX.GWLP_USERDATA, (IntPtr) 0xdeadbeef);

            return window;
    }
    public static nint get_hinstance()
    {
        return hinstance;
    }

    private static InputStruct ret;
    public static unsafe InputStruct pump_messages(bool wait_for_messages)
    {
        var old_stt = ret.pressed;
        ret = new()
        {
            pressed = old_stt,
            scanCodesPtr = scanCodeBuf
        };
        while (PInvoke.PeekMessage(out var msg, (HWND) 0,0,0,PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE))
        {
            // Console.WriteLine(
                // $"msg_id: 0x{msg.message:x}: {(WIN_EVENTS)msg.message}, wParam: {msg.wParam}, lParam: {msg.lParam}, time: {msg.time}, pt: {msg.pt}");

            if (msg.message == (uint) WIN_EVENTS.WM_PAINT)
            {
                break;
            }

           
            switch (msg.message)
            {
                // case WIN_EVENTS.WM_NCHITTEST:
                            
            }
            // PInvoke.TranslateMessage(ref msg);
            ret.mouseXSoft = msg.pt.X;
            ret.mouseYSoft = msg.pt.Y;
            PInvoke.DispatchMessage(msg);
            if (FUNCKİNG_BREAK)
            {
                Console.WriteLine("THE FUCK???!!!");

                FUNCKİNG_BREAK = false;
                break;
            }
        }

        return ret;
    }


}

public struct InputStruct
{
    public int mouseXSoft, mouseYSoft;
    public bool isRawRelative;
    public int mouseXRaw, mouseYRaw;
    public ulong down, up, pressed;
    public int scanCodeCount;
    public ulong mods;
    public int scrollX, scrollY;
    public unsafe ushort* scanCodesPtr;

}