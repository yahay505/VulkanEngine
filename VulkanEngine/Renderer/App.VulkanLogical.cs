using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static DeviceInfo.QueueFamilyIndices _familyIndices=>DeviceInfo.indices;

    private static unsafe void CreateLogicalDevice()
   {
        var uniqueQueueFamilies = new[] { _familyIndices.graphicsFamily!.Value, _familyIndices.presentFamily!.Value };
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
        var a = stackalloc long[200];
        var createInfo = new VkDeviceCreateInfo();
        createInfo.pQueueCreateInfos = queueCreateInfos;
        createInfo.enabledLayerCount = 0;
        createInfo.ppEnabledLayerNames = null;
        createInfo.pEnabledFeatures = &deviceFeatures;
        createInfo.ppEnabledExtensionNames = (sbyte**) SilkMarshal.StringArrayToPtr(DeviceInfo.selectedExtensionNames);
        createInfo.enabledExtensionCount = (uint) DeviceInfo.selectedExtensionNames.Count;
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

        vkCreateDevice(physicalDevice, &createInfo, null, out device)
            .Expect("failed to create logical device!");
        vkLoadDevice(device);
        
        vkGetDeviceQueue(device, _familyIndices.graphicsFamily!.Value, 0, out graphicsQueue);
        vkGetDeviceQueue(device, _familyIndices.presentFamily!.Value, 0, out presentQueue);
        vkGetDeviceQueue(device, _familyIndices.transferFamily!.Value, 0, out transferQueue);
        vkGetDeviceQueue(device, _familyIndices.computeFamily!.Value, 0, out computeQueue);
        
        
        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.ppEnabledLayerNames);
        }
    }
}