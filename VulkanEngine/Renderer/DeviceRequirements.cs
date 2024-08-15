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
        VK_KHR_PUSH_DESCRIPTOR_EXTENSION_NAME,
#if MAC
        "VK_KHR_portability_subset"
#endif
    };
    public static VkPhysicalDeviceDescriptorIndexingFeatures requiredIndexingFeatures= new()
    {
        descriptorBindingStorageBufferUpdateAfterBind = true,
        descriptorBindingUpdateUnusedWhilePending = true,
        descriptorBindingPartiallyBound = true,
        descriptorBindingVariableDescriptorCount = true,
        shaderSampledImageArrayNonUniformIndexing = true,
        shaderStorageBufferArrayNonUniformIndexing = true,
        shaderUniformBufferArrayNonUniformIndexing = true,
        runtimeDescriptorArray = true,
    };
    public static VkPhysicalDeviceFeatures requiredDeviceFeatures = new()
    {
        samplerAnisotropy = true,
        multiDrawIndirect = true,
        
        
    };
        


    public static int scoreDevice(DeviceInfo deviceInfo)
    {
        
        var score = 0;
        
        
        score += deviceInfo.properties.deviceType==VkPhysicalDeviceType.DiscreteGpu?2:0;
        score += deviceInfo.properties.deviceType == VkPhysicalDeviceType.IntegratedGpu ? 1 : 0;

        score += CheckOptionalExtension(deviceInfo, VK_EXT_MULTI_DRAW_EXTENSION_NAME, out deviceInfo.supportsMultiDraw) ? 1 : 0;
        score += CheckOptionalExtension(deviceInfo, VK_KHR_DRAW_INDIRECT_COUNT_EXTENSION_NAME, out deviceInfo.supportsCmdDrawIndexedIndirectCount) ? 2 : 0;

        return score;
    }

    private static bool CheckOptionalExtension(DeviceInfo deviceInfo, string item, out bool status)
    {
        if (!deviceInfo.availableExtensionNames.Contains(item))
        {
            status = false;
            return false;
        }
        status = true;
        deviceInfo.selectedExtensionNames.Add(item);
        return true;
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
        Debug.Log($"Total Vulkan devices: {deviceCount}");
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

        var validDeviceIds = stackalloc int[(int)deviceCount];
        var validDeviceCount = 0;
        for (int i = 0; i < deviceCount; i++)
        {
            if (RequiredDeviceExtensionsSupported(dev_res_list[i])&&DeviceHasRequiredProperties(dev_res_list[i]) && DeviceHasRequiredFeatures(dev_res_list[i]))
            {
                validDeviceIds[validDeviceCount++] = i;
            }
        }
        Debug.Log($"Valid Vulkan devices: {validDeviceCount}");
        if (validDeviceCount<=0)
        {
            throw new("failed to find GPUs with required parameters!");
        }
        
        
        var bestScore = -1;
        var bestDevice = dev_res_list[0];
        for (var i = 0; i < validDeviceCount; i++)
        {
            // ReSharper disable once LocalVariableHidesMember
            var device = dev_res_list[validDeviceIds[i]];

            var score = scoreDevice(device);
            Debug.Log($"Device \"{device.name}\" score: {score}");
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
    

    private static unsafe bool DeviceHasRequiredFeatures(DeviceInfo device)
    {
        VkPhysicalDeviceDescriptorIndexingFeatures a = device.DescriptorIndexingFeatures;
        return true;
    }

    private static bool DeviceHasRequiredProperties(DeviceInfo device)
    {
        return true;
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
        descriptorBindingStorageBufferUpdateAfterBind = true,
        descriptorBindingUpdateUnusedWhilePending = true,
    };
    var features = new VkPhysicalDeviceFeatures2()
   {
        pNext = &descriptorIndexingFeatures,
    };
    
    vkGetPhysicalDeviceFeatures2(device,&features);
    vkGetPhysicalDeviceProperties(device,out var props);
    deviceInfo.name = Marshal.PtrToStringAnsi((nint) props.deviceName)!;
    deviceInfo.properties = props;
    CheckDeviceExtensionSupport(ref deviceInfo);
    deviceInfo.features = features with {pNext = null};
    deviceInfo.DescriptorIndexingFeatures = descriptorIndexingFeatures;
    Debug.Log($"max sampler count: {props.limits.maxSamplerAllocationCount}");
    Debug.Log($"max alloc count: {props.limits.maxMemoryAllocationCount}");
    Debug.Log($"max set count: {props.limits.maxBoundDescriptorSets}");

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