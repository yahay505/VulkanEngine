namespace VulkanEngine;

/// <summary>
/// istihbarat ajansı
/// </summary>
public static class MIT
{
    public const OSType OS =
#if WINDOWS
        OSType.Windows;
#elif MAC
        OSType.Mac;
#elif LINUX
        OSType.Linux;
#else
        OSType.Unknown;
#endif
    // public static int CoreCount;
    
    
    
    public static void Start()
    {
        return;
        // var HI = new Hardware.Info.HardwareInfo();
        // HI.RefreshCPUList();
        // CoreCount = HI.CpuList.Sum(c=>c.CpuCoreList.Count);
    }

    public static Span<string> VulkanWindowingInstanceExtensions()
    {
            switch (OS)
            {
                    case OSType.Windows:
                            break;
                    case OSType.Mac:
                            return OSBindingTMP.MacBinding.GetInstanceRequirements();
                    case OSType.Linux:
                            break;
                    case OSType.Unknown:
                            break;
            }
    }

}
public enum OSType
{
        Windows,
        Mac,
        Linux,
        Unknown,
        Android
}