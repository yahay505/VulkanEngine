using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public class DeviceInfo
{
    public PhysicalDevice device;
    public PhysicalDeviceProperties properties;
    public bool supportsCmdDrawIndexedIndirectCount;
    public bool supportsMultiDraw;
    public PhysicalDeviceDescriptorIndexingFeatures DescriptorIndexingFeatures;
    public PhysicalDeviceFeatures2 features;
    public QueueFamilyIndices indices;
    public HashSet<string?> availableExtensionNames;
    public List<string> selectedExtensionNames=new();
    public int score;
    

    public DeviceInfo()
    {
    }

    public DeviceInfo(PhysicalDevice device)
    {
        this.device = device;
    }

    public struct QueueFamilyIndices
    {
        public uint? graphicsFamily { get; set; }
        public uint? presentFamily { get; set; }
        public uint? transferFamily { get; set; }
        public uint? computeFamily { get; set; }
        
        public bool IsComplete()
        {
            return graphicsFamily.HasValue&&
                   presentFamily.HasValue&&
                   transferFamily.HasValue&&
                   computeFamily.HasValue&&
                   true;
        }
    }
}