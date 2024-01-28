using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VulkanEngine;
/// <summary>
/// For all your OS level needs
/// </summary>
public static class ALO_CIMER
{
    
    [DllImport("Kernel32", EntryPoint = "GetCurrentThreadId", ExactSpelling = true)]
    public static extern Int32 GetCurrentWin32ThreadId();
    
    public static int GetCurrentBackingOSThreadID()
    {
        return GetCurrentWin32ThreadId();
    }
    
    public static void SetThreadAffinityMask(Thread t,nint mask)
    {
        return;
        int unmanagedId = 0;
        ProcessThread myThread = (from ProcessThread entry in Process.GetCurrentProcess().Threads
            where entry.Id == unmanagedId 
            select entry).Single();
#if WINDOWS
#pragma warning disable CA1416
        myThread.ProcessorAffinity = mask;
#pragma warning restore CA1416
#elif MAC
        throw new PlatformNotSupportedException();
#elif LINUX
        
#else
        throw new PlatformNotSupportedException();
#endif
        
    }

}