using System.Diagnostics;
using System.Runtime.InteropServices;
using Pastel;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanEngine.Renderer2.infra;

public static partial class Infra
{
    
    public static unsafe void CreateVkInstance()
    {
        var applicationInfo = new VkApplicationInfo()
            {
                //sType = VkStructureType.ApplicationInfo,
                apiVersion = new VkVersion(1, 2, 0),
                pEngineName = (sbyte*)Marshal.StringToHGlobalAnsi("Vengine"),
                //engineVersion = new VkVersion()
            };

            var validationLayerNames = new[]
            {
                "VK_LAYER_KHRONOS_validation",
            };

            var enabledLayerNames = new IntPtr[0];

            if (EnableValidationLayers)
            {
                var layers = vkEnumerateInstanceLayerProperties();
                var availableLayerNames = new HashSet<string>();

                for (var index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = properties.layerName;
                    var name = Marshal.PtrToStringAnsi((IntPtr)namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = validationLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();

                // Check if validation was really available
                
                Console.WriteLine($"Enabled Validation Layers: {enabledLayerNames.Length > 0}");
            }

            var extensionProperties = vkEnumerateInstanceExtensionProperties();
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (var index = 0; index < extensionProperties.Length; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = Marshal.PtrToStringAnsi((IntPtr)extensionProperty.extensionName);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add(VK_KHR_SURFACE_EXTENSION_NAME);
            if (!availableExtensionNames.Contains(VK_KHR_SURFACE_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {VK_KHR_SURFACE_EXTENSION_NAME} is not available");
            
            desiredExtensionNames.Add(VK_EXT_SURFACE_MAINTENANCE_1_EXTENSION_NAME);
            if (!availableExtensionNames.Contains(VK_EXT_SURFACE_MAINTENANCE_1_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {VK_EXT_SURFACE_MAINTENANCE_1_EXTENSION_NAME} is not available");
            desiredExtensionNames.Add(VK_KHR_GET_SURFACE_CAPABILITIES_2_EXTENSION_NAME);
            if (!availableExtensionNames.Contains(VK_KHR_GET_SURFACE_CAPABILITIES_2_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {VK_KHR_GET_SURFACE_CAPABILITIES_2_EXTENSION_NAME} is not available");

            if (MIT.OS == OSType.Windows)
            {
                desiredExtensionNames.Add(KHRWin32SurfaceExtensionName);
                if (!availableExtensionNames.Contains(KHRWin32SurfaceExtensionName))
                    throw new InvalidOperationException($"Required extension {KHRWin32SurfaceExtensionName} is not available");
            }
            else if (MIT.OS == OSType.Mac)
            {
                desiredExtensionNames.Add(VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
                desiredExtensionNames.Add(VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                desiredExtensionNames.Add("VK_KHR_portability_enumeration");
                if (!availableExtensionNames.Contains(VK_MVK_MACOS_SURFACE_EXTENSION_NAME) || !availableExtensionNames.Contains(VK_EXT_METAL_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {VK_MVK_MACOS_SURFACE_EXTENSION_NAME} or {VK_EXT_METAL_SURFACE_EXTENSION_NAME} is not available");
            }
            else if (MIT.OS == OSType.Android)
            {
                desiredExtensionNames.Add(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
                if (!availableExtensionNames.Contains(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {VK_KHR_ANDROID_SURFACE_EXTENSION_NAME} is not available");
            }
            else if (MIT.OS == OSType.Linux)
            {
                // if (availableExtensionNames.Contains("VK_KHR_xlib_surface"))
                // {
                //     desiredExtensionNames.Add("VK_KHR_xlib_surface");
                //     HasXlibSurfaceSupport = true;
                // }
                // else if (availableExtensionNames.Contains("VK_KHR_xcb_surface"))
                // {
                //     desiredExtensionNames.Add("VK_KHR_xcb_surface");
                // }
                // else
                // {
                //     throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface or VK_KHR_xlib_surface is available");
                // }
            }

            var enableDebugReport = EnableValidationLayers && availableExtensionNames.Contains(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
            if (enableDebugReport)
                desiredExtensionNames.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                fixed (void* enabledExtensionNamesPointer = &enabledExtensionNames[0])
                fixed (void* fEnabledLayerNames = enabledLayerNames) // null if array is empty or null
                {
                    var a = new VkDebugUtilsMessengerCreateInfoEXT
                    {
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning| VkDebugUtilsMessageSeverityFlagsEXT.Info| VkDebugUtilsMessageSeverityFlagsEXT.Verbose,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance| VkDebugUtilsMessageTypeFlagsEXT.DeviceAddressBinding,
                        pfnUserCallback = &DebugCallback
                        
                    };
                    
                    var instanceCreateInfo = new VkInstanceCreateInfo()
                    {
                        pApplicationInfo = &applicationInfo,
                        enabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        ppEnabledLayerNames = (sbyte**)fEnabledLayerNames,
                        enabledExtensionCount = (uint)enabledExtensionNames.Length,
                        ppEnabledExtensionNames = (sbyte**)enabledExtensionNamesPointer,
                        flags = MIT.OS==OSType.Mac?VkInstanceCreateFlags.EnumeratePortabilityKHR:VkInstanceCreateFlags.None ,
                        pNext = EnableValidationLayers?&a:default,
                    };

                    vkCreateInstance(&instanceCreateInfo, null, out API.instance);
                    vkLoadInstance(API.instance);
                }

                // Check if validation layer was available (otherwise detected count is 0)
                if (EnableValidationLayers)
                {
                    var createInfo = new VkDebugUtilsMessengerCreateInfoEXT()
                    {
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning| VkDebugUtilsMessageSeverityFlagsEXT.Info| VkDebugUtilsMessageSeverityFlagsEXT.Verbose,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance| VkDebugUtilsMessageTypeFlagsEXT.DeviceAddressBinding ,
                        pfnUserCallback = &DebugCallback
                    };
                    

                    vkCreateDebugUtilsMessengerEXT(API.instance, &createInfo, null, out var debugReportCallback).CheckResult();
                }
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }

                foreach (var enabledLayerName in enabledLayerNames)
                {
                    Marshal.FreeHGlobal(enabledLayerName);
                }

                Marshal.FreeHGlobal((IntPtr)applicationInfo.pEngineName);
            }
    }
    

    static string[] validationLayers = {"VK_LAYER_KHRONOS_validation"};
    public static unsafe bool CheckValidationLayerSupport()
    {

        uint layerCount = 0;
        vkEnumerateInstanceLayerProperties(&layerCount, null).Expect();
        var availableLayers = new VkLayerProperties[layerCount];
        fixed (VkLayerProperties* availableLayersPtr = availableLayers)
        {
            vkEnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer =>
        {
            return Marshal.PtrToStringAnsi((nint) layer.layerName);
        }).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }
    [UnmanagedCallersOnly]
    private static unsafe uint DebugCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes, VkDebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        var s = Marshal.PtrToStringAnsi((nint) pCallbackData->pMessage);
        if (s.StartsWith("Instance Extension:") || s.StartsWith("Device Extension:"))
        {
            return Vulkan.VK_FALSE;
        }

        if ((messageSeverity & VkDebugUtilsMessageSeverityFlagsEXT.Warning) != 0 &&
            (messageTypes & VkDebugUtilsMessageTypeFlagsEXT.Performance) == 0)
        {
            ;
        }

        if ((messageSeverity & VkDebugUtilsMessageSeverityFlagsEXT.Error) != 0)
        {
            Console.WriteLine(($"\nvalidation layer: " + s.Pastel(ConsoleColor.Red) + "\n" +
                               new StackTrace(true).ToString().Pastel(ConsoleColor.Gray)));
            ;
        }
        else
        {
            Console.WriteLine($"validation layer:" + s);
        }


//Debugger.Break();
        return Vulkan.VK_FALSE;
    }
}