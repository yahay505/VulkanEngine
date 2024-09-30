using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Vortice.Vulkan;
using VulkanEngine.Renderer;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static partial class Infra
{
    public static unsafe void CreateLogicalDevice(DeviceInfo chosenDevice)
   {
       var uniqueQueueFamilies = new[] { chosenDevice.indices.graphicsFamily!.Value, chosenDevice.indices.presentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(VkDeviceQueueCreateInfo))!;
        var queueCreateInfos = (VkDeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
           {
                queueFamilyIndex = uniqueQueueFamilies[i],
                queueCount = 1,
                pQueuePriorities = &queuePriority
            };
        }

        
        VkPhysicalDeviceFeatures deviceFeatures = DeviceRequirements.requiredDeviceFeatures;

        var length = (uint)uniqueQueueFamilies.Length;
        var createInfo = new VkDeviceCreateInfo();
        createInfo.pQueueCreateInfos = queueCreateInfos;
        createInfo.enabledLayerCount = 0;
        createInfo.ppEnabledLayerNames = null;
        createInfo.pEnabledFeatures = &deviceFeatures;
        createInfo.ppEnabledExtensionNames = (sbyte**) SilkMarshal.StringArrayToPtr(chosenDevice.selectedExtensionNames);
        createInfo.enabledExtensionCount = (uint) chosenDevice.selectedExtensionNames.Count;
        createInfo.pNext = Unsafe.AsPointer(ref DeviceRequirements.requiredIndexingFeatures);
        createInfo.flags = VkDeviceCreateFlags.None;
        
        createInfo.queueCreateInfoCount = length;
        if(length!=createInfo.queueCreateInfoCount)
            throw new Exception("AAAAAAAAAA");
        if (EnableValidationLayers)
        {
            createInfo.enabledLayerCount = (uint)validationLayers.Length;
            createInfo.ppEnabledLayerNames = (sbyte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.enabledLayerCount = 0;
        }

        vkCreateDevice(API.physicalDevice, &createInfo, null, out API.device)
            .Expect("failed to create logical device!");
        vkLoadDevice(API.device);
        
        vkGetDeviceQueue(API.device, chosenDevice.indices.graphicsFamily!.Value, 0, out API.graphicsQueue);
        vkGetDeviceQueue(API.device, chosenDevice.indices.graphicsFamily!.Value, 0, out API.presentQueue);// assume gfx can present
        vkGetDeviceQueue(API.device, chosenDevice.indices.transferFamily!.Value, 0, out API.transferQueue);
        vkGetDeviceQueue(API.device, chosenDevice.indices.computeFamily!.Value, 0, out API.computeQueue);
        
        
        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.ppEnabledLayerNames);
        }
    }
}