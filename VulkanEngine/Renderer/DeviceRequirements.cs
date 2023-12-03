using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
public static class DeviceRequirements
{



    
    private static readonly string[] requiredDeviceExtensions = {
        KhrSwapchain.ExtensionName,
        "VK_EXT_descriptor_indexing",
#if MAC
        "VK_KHR_portability_subset"
#endif
    };




    public static int scoreDevice(DeviceInfo deviceInfo)
    {
        var score = 0;
        
        
        score += deviceInfo.properties.DeviceType==PhysicalDeviceType.DiscreteGpu?2:0;
        score += deviceInfo.properties.DeviceType == PhysicalDeviceType.IntegratedGpu ? 1 : 0;
        // score += deviceInfo.features.Features.SamplerAnisotropy?1:0;
        // score += deviceInfo.DescriptorIndexingFeatures.DescriptorBindingStorageBufferUpdateAfterBind?1:0;
        // score += deviceInfo.DescriptorIndexingFeatures.DescriptorBindingUpdateUnusedWhilePending?1:0;
        // if (deviceInfo.availableExtensionNames.Contains(ExtMultiDraw.ExtensionName))
        // {
        //     deviceInfo.supportsMultiDraw = true;
        //     score += 1;
        // }
        if (deviceInfo.availableExtensionNames.Contains(ExtMultiDraw.ExtensionName))
        {
            deviceInfo.supportsMultiDraw = true;
            deviceInfo.selectedExtensionNames.Add(ExtMultiDraw.ExtensionName);
            score += 1;
        }
        if (deviceInfo.availableExtensionNames.Contains(KhrDrawIndirectCount.ExtensionName))
        {
            deviceInfo.supportsCmdDrawIndexedIndirectCount = true;
            deviceInfo.selectedExtensionNames.Add(KhrDrawIndirectCount.ExtensionName);
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
    

    public static unsafe DeviceInfo PickPhysicalDevice()
    {
            
        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        if (deviceCount==0)
        {
            throw new("failed to find GPUs with Vulkan support!");
        }
        var dev_res_list = new DeviceInfo[(int)deviceCount];
        
        var dev_list = stackalloc PhysicalDevice[(int) deviceCount];
        vk.EnumeratePhysicalDevices(instance, &deviceCount, dev_list);
        
        for (int i = 0; i < deviceCount; i++)
        {
            dev_res_list[i] = new DeviceInfo(device: dev_list[i]);
            GatherPhysicalDeviceData(dev_res_list[i]);
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
                   
    static unsafe void FindQueueFamilies(DeviceInfo deviceInfo)
    {
        ref var device =ref deviceInfo.device;
        ref var indices = ref deviceInfo.indices;

        uint queueFamilityCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilityCount];

        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamilies);
            
        for (uint i=0;i<queueFamilityCount;i++)
        {
            var queueFamily = queueFamilies[i];
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.graphicsFamily = i;
            }
            Bool32 presentSupport = false;
            khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, &presentSupport);
            if (presentSupport)
            {
                indices.presentFamily = i;
            }
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.TransferBit))
            {
                indices.transferFamily = i;
            }
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                indices.computeFamily = i;
            }
            // queueFamily.
            if (indices.IsComplete())
            {
                break;
            }
        }
        //get device name
        var properties = vk.GetPhysicalDeviceProperties(device);
        //write all families
        Console.WriteLine($"all families for device({properties.DeviceType.ToString()}) {SilkMarshal.PtrToString((nint) properties.DeviceName)} id:{properties.DeviceID}");
        Console.WriteLine();
        for (int i = 0; i < queueFamilityCount; i++)
        {
            Console.WriteLine($"queueFamily{i}: {queueFamilies[i].QueueFlags}");
        }
        Console.WriteLine();
        Console.WriteLine("selceted families:");
        Console.WriteLine($"graphicsFamily:{indices.graphicsFamily}");
        Console.WriteLine($"presentFamily:{indices.presentFamily}");
        Console.WriteLine($"transferFamily:{indices.transferFamily}");
        Console.WriteLine($"computeFamily:{indices.computeFamily}");
        Console.WriteLine();
    }

    static unsafe void GatherPhysicalDeviceData(DeviceInfo deviceInfo)
{
    
    ref var device = ref deviceInfo.device;
    FindQueueFamilies(deviceInfo);
    var descriptorIndexingFeatures = new PhysicalDeviceDescriptorIndexingFeatures()
    {
        SType = StructureType.PhysicalDeviceDescriptorIndexingFeatures,
    };
    var features = new PhysicalDeviceFeatures2()
    {
        SType = StructureType.PhysicalDeviceFeatures2,
        PNext = &descriptorIndexingFeatures,
    };
    
    vk.GetPhysicalDeviceFeatures2(device,&features);
    deviceInfo.properties = vk.GetPhysicalDeviceProperties(device);

    CheckDeviceExtensionSupport(ref deviceInfo);
    deviceInfo.features = features with {PNext = null};
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
        vk.EnumerateDeviceExtensionProperties(device, ((byte*)null)!, &extensionCount, null);
        var avaliableExtension = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* avaliableExtensionPtr = avaliableExtension)
            vk.EnumerateDeviceExtensionProperties(device, ((byte*) null)!, ref extensionCount, avaliableExtensionPtr);
        deviceInfo.availableExtensionNames = avaliableExtension.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();
    }

}
}