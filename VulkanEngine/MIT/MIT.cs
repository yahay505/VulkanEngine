using System.Diagnostics;

namespace VulkanEngine;

public static class MIT
{
    public static int CoreCount;
    
    public static void Start()
    {
        var HI = new Hardware.Info.HardwareInfo();
        HI.RefreshAll();
        CoreCount = HI.CpuList.Sum(c=>c.CpuCoreList.Count);

        
    }
}