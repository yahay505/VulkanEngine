using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private static DeviceInfo.QueueFamilyIndices _familyIndices=>DeviceInfo.indices;

    private static unsafe void CreateLogicalDevice()
    {

        var uniqueQueueFamilies = new[] { _familyIndices.graphicsFamily!.Value, _familyIndices.presentFamily!.Value };
        uniqueQueueFamilies = uniqueQueueFamilies.Distinct().ToArray();

        using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
        var queueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref mem.GetPinnableReference());

        float queuePriority = 1.0f;
        for (int i = 0; i < uniqueQueueFamilies.Length; i++)
        {
            queueCreateInfos[i] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = uniqueQueueFamilies[i],
                QueueCount = 1,
                PQueuePriorities = &queuePriority
            };
        }

        PhysicalDeviceFeatures deviceFeatures = new PhysicalDeviceFeatures
        {
            SamplerAnisotropy = true,
            MultiDrawIndirect = true
        };
        var next = new PhysicalDeviceDescriptorIndexingFeatures()
        {
            SType = StructureType.PhysicalDeviceDescriptorIndexingFeatures,
DescriptorBindingStorageBufferUpdateAfterBind = true,
DescriptorBindingUpdateUnusedWhilePending = true,
        };
        DeviceCreateInfo createInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
            PQueueCreateInfos = queueCreateInfos,

            PEnabledFeatures = &deviceFeatures,
            PpEnabledExtensionNames = (byte**) SilkMarshal.StringArrayToPtr( DeviceInfo.selectedExtensionNames),
            EnabledExtensionCount = (uint) DeviceInfo.selectedExtensionNames.Count,
            PNext = &next,
        };

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
        }

        vk.CreateDevice(physicalDevice, in createInfo, null, out device)
            .Expect("failed to create logical device!");

        vk.GetDeviceQueue(device, _familyIndices.graphicsFamily!.Value, 0, out graphicsQueue);
        vk.GetDeviceQueue(device, _familyIndices.presentFamily!.Value, 0, out presentQueue);
        vk.GetDeviceQueue(device, _familyIndices.transferFamily!.Value, 0, out transferQueue);
        vk.GetDeviceQueue(device, _familyIndices.computeFamily!.Value, 0, out computeQueue);
        
        
        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }
}