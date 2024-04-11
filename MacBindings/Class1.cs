// using System.Runtime.InteropServices;
// using Vortice.Vulkan;
// using static Vortice.Vulkan.Vulkan;
// namespace MacBindings;
//
// public class Class1
// {
//     public static void GetWindow()
//     {
//         var window = new UIKit.UIWindow();
//
//         Thread.Sleep(10000);
//         // VkMacOSSurfaceCreateInfoMVK
//     }
//
//     public static unsafe void test()
//     {
//         vkInitialize();
//         var instanceinfo= new VkInstanceCreateInfo()
//         {
//             
//         };
//         vkCreateInstance(&instanceinfo, null, out var vk);
//         var info = new VkMacOSSurfaceCreateInfoMVK
//         {
//         };
//         
//         
//         VkApplicationInfo appInfo = new()
//         {
//             pApplicationName = (sbyte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
//             applicationVersion = new (1, 0, 0),
//             pEngineName = (sbyte*)Marshal.StringToHGlobalAnsi("No Engine"),
//             engineVersion = new (1, 0, 0),
//             apiVersion = new VkVersion(1,2,0),
//         };
//
//         VkInstanceCreateInfo createInfo = new();
//         
//         createInfo.pApplicationInfo = &appInfo;
//
//         var extensions =stackalloc[]{(sbyte*)Marshal.StringToHGlobalAnsi(VK_KHR_PORTABILITY_ENUMERATION_EXTENSION_NAME)};
//         
//         createInfo.enabledExtensionCount = 1;
//         createInfo.ppEnabledExtensionNames = extensions;
//
//         createInfo.flags |= VkInstanceCreateFlags.EnumeratePortabilityKHR;
//         
//         createInfo.enabledLayerCount = 0;
//         createInfo.pNext = null;
//         
//
//         if (vkCreateInstance(&createInfo, null, out var instance) != VkResult.Success)
//         {
//             throw new Exception("failed to create instance!");
//         }
//
//         Marshal.FreeHGlobal((IntPtr)appInfo.pApplicationName);
//         Marshal.FreeHGlobal((IntPtr)appInfo.pEngineName);
//         Marshal.FreeHGlobal((nint)createInfo.ppEnabledExtensionNames[0]);
//
//
//
//         // vkCreateMacOSSurfaceMVK();
//
//
//
//
//
//
//     }
// }