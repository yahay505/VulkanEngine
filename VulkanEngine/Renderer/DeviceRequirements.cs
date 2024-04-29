using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public static class DeviceRequirements
{



    
    private static readonly string[] requiredDeviceExtensions = {
        VK_KHR_SWAPCHAIN_EXTENSION_NAME,
        "VK_EXT_descriptor_indexing",
#if MAC
        "VK_KHR_portability_subset"
#endif
    };




    public static int scoreDevice(DeviceInfo deviceInfo)
    {
        var score = 0;
        
        
        score += deviceInfo.properties.deviceType==VkPhysicalDeviceType.DiscreteGpu?2:0;
        score += deviceInfo.properties.deviceType == VkPhysicalDeviceType.IntegratedGpu ? 1 : 0;
        // score += deviceInfo.features.Features.SamplerAnisotropy?1:0;
        // score += deviceInfo.DescriptorIndexingFeatures.DescriptorBindingStorageBufferUpdateAfterBind?1:0;
        // score += deviceInfo.DescriptorIndexingFeatures.DescriptorBindingUpdateUnusedWhilePending?1:0;
        // if (deviceInfo.availableExtensionNames.Contains(ExtMultiDraw.ExtensionName))
        // {
        //     deviceInfo.supportsMultiDraw = true;
        //     score += 1;
        // }
        if (deviceInfo.availableExtensionNames.Contains(VK_EXT_MULTI_DRAW_EXTENSION_NAME))
        {
            deviceInfo.supportsMultiDraw = true;
            deviceInfo.selectedExtensionNames.Add(VK_EXT_MULTI_DRAW_EXTENSION_NAME);
            score += 1;
        }
        if (deviceInfo.availableExtensionNames.Contains(VK_KHR_DRAW_INDIRECT_COUNT_EXTENSION_NAME))
        {
            deviceInfo.supportsCmdDrawIndexedIndirectCount = true;
            deviceInfo.selectedExtensionNames.Add(VK_KHR_DRAW_INDIRECT_COUNT_EXTENSION_NAME);
            score += 2;
        }
        return score;
    }
    public static bool RequiredDeviceExtensionsSupported(DeviceInfo deviceInfo)
    {
        var availableExtensions = deviceInfo.availableExtensionNames;
        foreach (var requiredDeviceExtension in requiredDeviceExtensions)
        {
            if (!availableExtensions.Contains(requiredDeviceExtension))
            {
                return false;
            }
        }
        return true;
    }
    

    public static unsafe DeviceInfo PickPhysicalDevice(VkSurfaceKHR surface)
    {
            
        uint deviceCount = 0;
        vkEnumeratePhysicalDevices(instance, &deviceCount, null);
        if (deviceCount==0)
        {
            throw new("failed to find GPUs with Vulkan support!");
        }
        var dev_res_list = new DeviceInfo[(int)deviceCount];
        
        var dev_list = stackalloc VkPhysicalDevice[(int) deviceCount];
        vkEnumeratePhysicalDevices(instance, &deviceCount, dev_list);
        
        for (int i = 0; i < deviceCount; i++)
        {
            dev_res_list[i] = new DeviceInfo(device: dev_list[i]);
            GatherPhysicalDeviceData(dev_res_list[i], surface);
        }
        var bestScore = -1;
        var bestDevice = dev_res_list[0];
        for (var i = 0; i < deviceCount; i++)
        {
            // ReSharper disable once LocalVariableHidesMember
            var device = dev_res_list[i];

            var score = scoreDevice(device);
            if (!device.indices.IsComplete() || !RequiredDeviceExtensionsSupported(device))
            {
                score = -1;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestDevice = device;
            }
        }

        if (bestScore < 0)
        {
            throw new("failed to find a suitable GPU!");
        }

        bestDevice.selectedExtensionNames.AddRange(requiredDeviceExtensions);
        return bestDevice;
    }
                   
    static unsafe void FindQueueFamilies(DeviceInfo deviceInfo, VkSurfaceKHR surface)
    {
        uint queueFamilityCount = 0;
        vkGetPhysicalDeviceQueueFamilyProperties(deviceInfo.device, & queueFamilityCount, null);

        var queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilityCount];

        vkGetPhysicalDeviceQueueFamilyProperties(deviceInfo.device, & queueFamilityCount, queueFamilies);
            
        for (uint i=0;i<queueFamilityCount;i++)
        {
            var queueFamily = queueFamilies[i];
            if (queueFamily.queueFlags.HasFlag(VkQueueFlags.Graphics))
            {
                deviceInfo.indices.graphicsFamily = i;
            }
            VkBool32 presentSupport = false;
            vkGetPhysicalDeviceSurfaceSupportKHR(deviceInfo.device, i, surface, &presentSupport);
            if (presentSupport)
            {
                deviceInfo.indices.presentFamily = i;
            }
            if (queueFamily.queueFlags.HasFlag(VkQueueFlags.Transfer))
            { 
                deviceInfo.indices.transferFamily = i;
            }
            if (queueFamily.queueFlags.HasFlag(VkQueueFlags.Compute))
            {
                deviceInfo.indices.computeFamily = i;
            }
            // queueFamily.
            if (deviceInfo.indices.IsComplete())
            {
                break;
            }
        }
        //get device name
        VkPhysicalDeviceProperties properties;
        vkGetPhysicalDeviceProperties(deviceInfo.device, &properties);
        //write all families
        Console.WriteLine($"all families for device({properties.deviceType.ToString()}) {SilkMarshal.PtrToString((nint) properties.deviceName)} id:{properties.deviceID}");
        Console.WriteLine();
        for (int i = 0; i < queueFamilityCount; i++)
        {
            Console.WriteLine($"queueFamily{i}: {queueFamilies[i].queueFlags}");
        }
        Console.WriteLine();
        Console.WriteLine("selceted families:");
        Console.WriteLine($"graphicsFamily:{deviceInfo.indices.graphicsFamily}");
        Console.WriteLine($"presentFamily:{deviceInfo.indices.presentFamily}");
        Console.WriteLine($"transferFamily:{deviceInfo.indices.transferFamily}");
        Console.WriteLine($"computeFamily:{deviceInfo.indices.computeFamily}");
        Console.WriteLine();
    }

    static unsafe void GatherPhysicalDeviceData(DeviceInfo deviceInfo, VkSurfaceKHR surface)
{
    
    ref var device = ref deviceInfo.device;
    FindQueueFamilies(deviceInfo,surface);
    var descriptorIndexingFeatures = new VkPhysicalDeviceDescriptorIndexingFeatures()
   {
    };
    var features = new VkPhysicalDeviceFeatures2()
   {
        pNext = &descriptorIndexingFeatures,
    };
    
    vkGetPhysicalDeviceFeatures2(device,&features); 
    vkGetPhysicalDeviceProperties(device,out deviceInfo.properties);

    CheckDeviceExtensionSupport(ref deviceInfo);
    deviceInfo.features = features with {pNext = null};
    deviceInfo.DescriptorIndexingFeatures = descriptorIndexingFeatures;


    return;
        
        // deviceInfo.indices.IsComplete()&& extensionsSupported&& features.Features.SamplerAnisotropy
           // && ((PhysicalDeviceDescriptorIndexingFeatures*)features.PNext)->DescriptorBindingStorageBufferUpdateAfterBind
           // && ((PhysicalDeviceDescriptorIndexingFeatures*)features.PNext)->DescriptorBindingUpdateUnusedWhilePending
        // ;

}
    private static unsafe void CheckDeviceExtensionSupport(ref DeviceInfo deviceInfo)
    {
        ref var device = ref deviceInfo.device;
        uint extensionCount = 0;
        vkEnumerateDeviceExtensionProperties(device, ((sbyte*)null)!, &extensionCount, null);
        var avaliableExtension = new VkExtensionProperties[extensionCount];
        fixed (VkExtensionProperties* avaliableExtensionPtr = avaliableExtension)
            vkEnumerateDeviceExtensionProperties(device, ((sbyte*) null)!, & extensionCount, avaliableExtensionPtr);
        deviceInfo.availableExtensionNames = avaliableExtension.Select(extension =>
        {
            return Marshal.PtrToStringAnsi((IntPtr) extension.extensionName);
        }).ToHashSet();
    }

}
}