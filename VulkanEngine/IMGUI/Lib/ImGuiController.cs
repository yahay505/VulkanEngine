// // Licensed to the .NET Foundation under one or more agreements.
// // The .NET Foundation licenses this file to you under the MIT license.
//
// using System.Numerics;
// using System.Runtime.CompilerServices;
// using System.Runtime.InteropServices;
// using ImGuiNET;
// using Silk.NET.Core.Native;
// using Silk.NET.Maths;
// using Vortice.Vulkan;
// using VulkanEngine;
// using VulkanEngine.Renderer;
// using Point = System.Drawing.Point;
// using Result=Vortice.Vulkan.VkResult;
// using static Vortice.Vulkan.Vulkan;
//
// namespace Silk.NET.Vulkan.Extensions.ImGui
// {
//     public class ImGuiController : IDisposable
//     {
//         private IView _view;
//         private IInputContext _input;
//         private VkDevice _device;
//         private VkPhysicalDevice _physicalDevice;
//         private bool _frameBegun;
//         private readonly List<char> _pressedChars = new List<char>();
//         private IKeyboard _keyboard;
//         private VkDescriptorPool _descriptorPool;
//         private VkRenderPass _renderPass;
//         private int _windowWidth;
//         private int _windowHeight;
//         private int _swapChainImageCt;
//         private VkSampler _fontSampler;
//         private VkDescriptorSetLayout _descriptorSetLayout;
//         private VkDescriptorSet _descriptorSet;
//         private VkPipelineLayout _pipelineLayout;
//         private VkShaderModule _shaderModuleVert;
//         private VkShaderModule _shaderModuleFrag;
//         private VkPipeline _pipeline;
//         private WindowRenderBuffers _mainWindowRenderBuffers;
//         private GlobalMemory _frameRenderBuffers;
//         private VkDeviceMemory _fontMemory;
//         private VkImage _fontImage;
//         private VkImageView _fontView;
//         private ulong _bufferMemoryAlignment = 256;
//         private static readonly Key[] KeyValues = Enum.GetValues<Key>();
//
//         /// <summary>
//         /// Constructs a new ImGuiController.
//         /// </summary>
//         /// <param name="view">Window view</param>
//         /// <param name="input">Input context</param>
//         /// <param name="physicalDevice">The physical device instance in use</param>
//         /// <param name="graphicsFamilyIndex">The graphics family index corresponding to the graphics queue</param>
//         /// <param name="swapChainImageCt">The number of images used in the swap chain</param>
//         /// <param name="swapChainFormat">The image format used by the swap chain</param>
//         /// <param name="depthBufferFormat">The image formate used by the depth buffer, or null if no depth buffer is used</param>
//         /// <param name="device"></param>
//         /// <param name="vk">The vulkan api instance</param>
//         public ImGuiController(IView view, IInputContext input, VkPhysicalDevice physicalDevice,
//             uint graphicsFamilyIndex, int swapChainImageCt, VkFormat swapChainFormat, VkFormat? depthBufferFormat,
//             VkDevice device)
//         {
//             var context = ImGuiNET.ImGui.CreateContext();
//             ImGuiNET.ImGui.SetCurrentContext(context);
//
//             // Use the default font
//             var io = ImGuiNET.ImGui.GetIO();
//             io.Fonts.AddFontDefault();
//             io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
//
//             Init( view, input, physicalDevice, graphicsFamilyIndex, swapChainImageCt, swapChainFormat, depthBufferFormat,device);
//
//             SetKeyMappings();
//
//             SetPerFrameImGuiData(1f / 60f);
//
//             BeginFrame();
//         }
//
//         /// <summary>
//         /// Constructs a new ImGuiController.
//         /// </summary>
//         /// <param name="view">Window view</param>
//         /// <param name="input">Input context</param>
//         /// <param name="imGuiFontConfig">A custom ImGui configuration</param>
//         /// <param name="physicalDevice">The physical device instance in use</param>
//         /// <param name="graphicsFamilyIndex">The graphics family index corresponding to the graphics queue</param>
//         /// <param name="swapChainImageCt">The number of images used in the swap chain</param>
//         /// <param name="swapChainFormat">The image format used by the swap chain</param>
//         /// <param name="depthBufferFormat">The image formate used by the depth buffer, or null if no depth buffer is used</param>
//         /// <param name="device"></param>
//         /// <param name="vk">The vulkan api instance</param>
//         public unsafe ImGuiController(IView view, IInputContext input, ImGuiFontConfig imGuiFontConfig,
//             VkPhysicalDevice physicalDevice, uint graphicsFamilyIndex, int swapChainImageCt, VkFormat swapChainFormat,
//             VkFormat? depthBufferFormat, VkDevice device)
//         {
//             
//             var context = ImGuiNET.ImGui.CreateContext();
//             ImGuiNET.ImGui.SetCurrentContext(context);
//
//             // Upload custom ImGui font
//             var io = ImGuiNET.ImGui.GetIO();
//             io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
//             if (io.Fonts.AddFontFromFileTTF(imGuiFontConfig.FontPath, imGuiFontConfig.FontSize).NativePtr == default)
//             {
//                 throw new Exception($"Failed to load ImGui font");
//             }
//
//             Init( view, input, physicalDevice, graphicsFamilyIndex, swapChainImageCt, swapChainFormat, depthBufferFormat,device);
//
//             SetKeyMappings();
//
//             SetPerFrameImGuiData(1f / 60f);
//
//             BeginFrame();
//         }
//
//         private unsafe void Init(IView view, IInputContext input, VkPhysicalDevice physicalDevice,
//             uint graphicsFamilyIndex, int swapChainImageCt, VkFormat swapChainFormat, VkFormat? depthBuf ,
//             VkDevice device)
//         {
//             _view = view;
//             _input = input;
//             _physicalDevice = physicalDevice;
//             _windowWidth = view.Size.X;
//             _windowHeight = view.Size.Y;
//             _swapChainImageCt = swapChainImageCt;
//
//             if (swapChainImageCt < 2)
//             {
//                 throw new Exception("Swap chain image count must be >= 2");
//             }
//             //
//             // if (!vkCurrentDevice.HasValue)
//             // {
//             //     throw new InvalidOperationException("vkCurrentDevice is null. vkCurrentDevice must be set to the current device.");
//             // }
//
//             _device = device;
//
//             // Set default style
//             ImGuiNET.ImGui.StyleColorsDark();
//
//             // Create the descriptor pool for ImGui
//             Span<VkDescriptorPoolSize> poolSizes = stackalloc VkDescriptorPoolSize[] { new VkDescriptorPoolSize(VkDescriptorType.CombinedImageSampler, 1) };
//             var descriptorPool = new VkDescriptorPoolCreateInfo();
//             descriptorPool.poolSizeCount = (uint)poolSizes.Length;
//             descriptorPool.pPoolSizes = (VkDescriptorPoolSize*)Unsafe.AsPointer(ref poolSizes.GetPinnableReference());
//             descriptorPool.maxSets = 1;
//             if (vkCreateDescriptorPool(_device, &descriptorPool, default, out _descriptorPool) != Result.Success)
//             {
//                 throw new Exception("Unable to create descriptor pool");
//             }
//
//             // // Create the render pass
//             // var colorAttachment = new AttachmentDescription();
//             // colorAttachment.format = swapChainFormat;
//             // colorAttachment.Samples = SampleCountFlags.Count1;
//             // colorAttachment.LoadOp = AttachmentLoadOp.Load;
//             // colorAttachment.StoreOp = AttachmentStoreOp.Store;
//             // colorAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
//             // colorAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
//             // colorAttachment.InitialLayout = AttachmentLoadOp.Load == AttachmentLoadOp.Clear ? ImageLayout.Undefined : ImageLayout.PresentSrcKhr;
//             // colorAttachment.FinalLayout = ImageLayout.PresentSrcKhr;
//             //
//             // var colorAttachmentRef = new AttachmentReference();
//             // colorAttachmentRef.Attachment = 0;
//             // colorAttachmentRef.Layout = ImageLayout.ColorAttachmentOptimal;
//             //
//             // var subpass = new SubpassDescription();
//             // subpass.PipelineBindPoint = PipelineBindPoint.Graphics;
//             // subpass.ColorAttachmentCount = 1;
//             // subpass.PColorAttachments = (AttachmentReference*)Unsafe.AsPointer(ref colorAttachmentRef);
//             //
//             // Span<AttachmentDescription> attachments = stackalloc AttachmentDescription[] { colorAttachment };
//             // var depthAttachment = new AttachmentDescription();
//             // var depthAttachmentRef = new AttachmentReference();
//             // if (depthBufferFormat.HasValue)
//             // {
//             //     depthAttachment.format = depthBufferFormat.Value;
//             //     depthAttachment.Samples = SampleCountFlags.Count1;
//             //     depthAttachment.LoadOp = AttachmentLoadOp.Load;
//             //     depthAttachment.StoreOp = AttachmentStoreOp.DontCare;
//             //     depthAttachment.StencilLoadOp = AttachmentLoadOp.DontCare;
//             //     depthAttachment.StencilStoreOp = AttachmentStoreOp.DontCare;
//             //     depthAttachment.InitialLayout = AttachmentLoadOp.Load == AttachmentLoadOp.Clear ? ImageLayout.Undefined : ImageLayout.DepthStencilAttachmentOptimal;
//             //     depthAttachment.FinalLayout = ImageLayout.DepthStencilAttachmentOptimal;
//             //
//             //     depthAttachmentRef.Attachment = 1;
//             //     depthAttachmentRef.Layout = ImageLayout.DepthStencilAttachmentOptimal;
//             //
//             //     subpass.PDepthStencilAttachment = (AttachmentReference*)Unsafe.AsPointer(ref depthAttachmentRef);
//             //
//             //     attachments = stackalloc AttachmentDescription[] { colorAttachment, depthAttachment };
//             // }
//
//             // var dependency = new SubpassDependency();
//             // dependency.SrcSubpass = Vk.SubpassExternal;
//             // dependency.dstSubpass = 0;
//             // dependency.srcStageMask = VkPipelineStageFlags.ColorAttachmentOutputBit
//             //     | VkPipelineStageFlags.EarlyFragmentTestsBit
//             //     
//             //     ;
//             // dependency.srcAccessMask = 0;
//             // dependency.dstStageMask = VkPipelineStageFlags.ColorAttachmentOutputBit
//             //                           | VkPipelineStageFlags.EarlyFragmentTestsBit
//             //     ;
//             // dependency.dstAccessMask = VkAccessFlags.DepthStencilAttachmentWrite | VkAccessFlags.ColorAttachmentWrite;
//             //
//             // var renderPassInfo = new RenderPassCreateInfo();
//             // renderPassInfo.SType = StructureType.RenderPassCreateInfo;
//             // renderPassInfo.AttachmentCount = (uint)attachments.Length;
//             // renderPassInfo.PAttachments = (AttachmentDescription*)Unsafe.AsPointer(ref attachments.GetPinnableReference());
//             // renderPassInfo.SubpassCount = 1;
//             // renderPassInfo.PSubpasses = (SubpassDescription*)Unsafe.AsPointer(ref subpass);
//             // renderPassInfo.DependencyCount = 1;
//             // renderPassInfo.PDependencies = (SubpassDependency*)Unsafe.AsPointer(ref dependency);
//
//             // if (vkCreateRenderPass(_device, renderPassInfo, default, out _renderPass) != Result.Success)
//             // {
//             //     throw new Exception($"Failed to create render pass");
//             // }
//
//             _renderPass = VKRender.RenderPass;
//             
//             
//             
//             
//             
//
//             var info = new VkSamplerCreateInfo();
//             info.magFilter = VkFilter.Linear;
//             info.minFilter = VkFilter.Linear;
//             info.mipmapMode = VkSamplerMipmapMode.Linear;
//             info.addressModeU = VkSamplerAddressMode.Repeat;
//             info.addressModeV = VkSamplerAddressMode.Repeat;
//             info.addressModeW = VkSamplerAddressMode.Repeat;
//             info.minLod = -1000;
//             info.maxLod = 1000;
//             info.maxAnisotropy = 1.0f;
//             if (vkCreateSampler(_device, &info, default, out _fontSampler) != Result.Success)
//             {
//                 throw new Exception("Unable to create sampler");
//             }
//
//             var sampler = _fontSampler;
//
//             var binding = new VkDescriptorSetLayoutBinding();
//             binding.descriptorType = VkDescriptorType.CombinedImageSampler;
//             binding.descriptorCount = 1;
//             binding.stageFlags = VkShaderStageFlags.Fragment;
//             binding.pImmutableSamplers = (VkSampler*)Unsafe.AsPointer(ref sampler);
//
//             var descriptorInfo = new VkDescriptorSetLayoutCreateInfo();
//             descriptorInfo.bindingCount = 1;
//             descriptorInfo.pBindings = (VkDescriptorSetLayoutBinding*)Unsafe.AsPointer(ref binding);
//             if (vkCreateDescriptorSetLayout(_device, &descriptorInfo, null, out _descriptorSetLayout) != Result.Success)
//             {
//                 throw new Exception("Unable to create descriptor set layout");
//             }
//
//             fixed (VkDescriptorSetLayout* pg_DescriptorSetLayout = &_descriptorSetLayout)
//             {
//                 var alloc_info = new VkDescriptorSetAllocateInfo();
//                 alloc_info.descriptorPool = _descriptorPool;
//                 alloc_info.descriptorSetCount = 1;
//                 alloc_info.pSetLayouts = pg_DescriptorSetLayout;
//                 VkDescriptorSet DescriptorSet = new VkDescriptorSet();
//                 vkAllocateDescriptorSets(_device, &alloc_info, &DescriptorSet).Expect("Unable to create descriptor sets");
//                 _descriptorSet = DescriptorSet;
//             }
//
//             var vertPushConst = new VkPushConstantRange();
//             vertPushConst.stageFlags = VkShaderStageFlags.Vertex;
//             vertPushConst.offset = sizeof(float) * 0;
//             vertPushConst.size = sizeof(float) * 4;
//
//             var set_layout = _descriptorSetLayout;
//             var layout_info = new VkPipelineLayoutCreateInfo();
//             layout_info.setLayoutCount = 1;
//             layout_info.pSetLayouts = (VkDescriptorSetLayout*)Unsafe.AsPointer(ref set_layout);
//             layout_info.pushConstantRangeCount = 1;
//             layout_info.pPushConstantRanges = (VkPushConstantRange*)Unsafe.AsPointer(ref vertPushConst);
//             vkCreatePipelineLayout(_device, &layout_info, null, out _pipelineLayout)
//                 .Expect("Unable to create the descriptor set layout");
//
//                 // Create the shader modules
//             if (_shaderModuleVert.Handle == default)
//             {
//                 fixed (uint* vertShaderBytes = &Shaders.VertexShader[0])
//                 {
//                     var vert_info = new VkShaderModuleCreateInfo();
//                     vert_info.codeSize = (nuint)Shaders.VertexShader.Length * sizeof(uint);
//                     vert_info.pCode = vertShaderBytes;
//                     vkCreateShaderModule(_device, &vert_info, null, out _shaderModuleVert)
//                         .Expect("Unable to create the vertex shader");
//                     
//                 }
//             }
//             if (_shaderModuleFrag.Handle == default)
//             {
//                 fixed (uint* fragShaderBytes = &Shaders.FragmentShader[0])
//                 {
//                     var frag_info = new VkShaderModuleCreateInfo();
//                     frag_info.codeSize = (nuint)Shaders.FragmentShader.Length * sizeof(uint);
//                     frag_info.pCode = fragShaderBytes;
//                      vkCreateShaderModule(_device, &frag_info, null, out _shaderModuleFrag)
//                         .Expect("Unable to create the fragment shader");
//                 }
//             }
//
//             // Create the pipeline
//             var stage = stackalloc VkPipelineShaderStageCreateInfo[2];
//             stage[0].stage = VkShaderStageFlags.Vertex;
//             stage[0].module = _shaderModuleVert;
//             stage[0].pName = (sbyte*)SilkMarshal.StringToPtr("main");
//             stage[1].stage = VkShaderStageFlags.Fragment;
//             stage[1].module = _shaderModuleFrag;
//             stage[1].pName = (sbyte*)SilkMarshal.StringToPtr("main");
//
//             var binding_desc = new VkVertexInputBindingDescription();
//             binding_desc.stride = (uint)Unsafe.SizeOf<ImDrawVert>();
//             binding_desc.inputRate = VkVertexInputRate.Vertex;
//
//             Span<VkVertexInputAttributeDescription> attribute_desc = stackalloc VkVertexInputAttributeDescription[3];
//             attribute_desc[0].location = 0;
//             attribute_desc[0].binding = binding_desc.binding;
//             attribute_desc[0].format = VkFormat.R32G32Sfloat;
//             attribute_desc[0].offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos));
//             attribute_desc[1].location = 1;
//             attribute_desc[1].binding = binding_desc.binding;
//             attribute_desc[1].format = VkFormat.R32G32Sfloat;
//             attribute_desc[1].offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv));
//             attribute_desc[2].location = 2;
//             attribute_desc[2].binding = binding_desc.binding;
//             attribute_desc[2].format = VkFormat.R8G8B8A8Unorm;
//             attribute_desc[2].offset = (uint)Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col));
//
//             var vertex_info = new VkPipelineVertexInputStateCreateInfo();
//             vertex_info.vertexBindingDescriptionCount = 1;
//             vertex_info.pVertexBindingDescriptions = (VkVertexInputBindingDescription*)Unsafe.AsPointer(ref binding_desc);
//             vertex_info.vertexAttributeDescriptionCount = 3;
//             vertex_info.pVertexAttributeDescriptions = (VkVertexInputAttributeDescription*)Unsafe.AsPointer(ref attribute_desc[0]);
//
//             var ia_info = new VkPipelineInputAssemblyStateCreateInfo();
//             ia_info.topology = VkPrimitiveTopology.TriangleList;
//
//             var viewport_info = new VkPipelineViewportStateCreateInfo();
//             viewport_info.viewportCount = 1;
//             viewport_info.scissorCount = 1;
//
//             var raster_info = new VkPipelineRasterizationStateCreateInfo();
//             raster_info.polygonMode = VkPolygonMode.Fill;
//             raster_info.cullMode = VkCullModeFlags.None;
//             raster_info.frontFace = VkFrontFace.CounterClockwise;
//             raster_info.lineWidth = 1.0f;
//
//             var ms_info = new VkPipelineMultisampleStateCreateInfo();
//             ms_info.rasterizationSamples = VkSampleCountFlags.Count1;
//
//             var color_attachment = new VkPipelineColorBlendAttachmentState();
//             color_attachment.blendEnable = new VkBool32(true);
//             color_attachment.srcColorBlendFactor = VkBlendFactor.SrcAlpha;
//             color_attachment.dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
//             color_attachment.colorBlendOp = VkBlendOp.Add;
//             color_attachment.srcAlphaBlendFactor = VkBlendFactor.One;
//             color_attachment.dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha;
//             color_attachment.alphaBlendOp = VkBlendOp.Add;
//             color_attachment.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
//
//             var depth_info = new VkPipelineDepthStencilStateCreateInfo();
//
//             var blend_info = new VkPipelineColorBlendStateCreateInfo();
//             blend_info.attachmentCount = 1;
//             blend_info.pAttachments = (VkPipelineColorBlendAttachmentState*)Unsafe.AsPointer(ref color_attachment);
//
//             Span<VkDynamicState> dynamic_states = stackalloc VkDynamicState[] { VkDynamicState.Viewport, VkDynamicState.Scissor };
//             var dynamic_state = new VkPipelineDynamicStateCreateInfo();
//             dynamic_state.dynamicStateCount = (uint)dynamic_states.Length;
//             dynamic_state.pDynamicStates = (VkDynamicState*)Unsafe.AsPointer(ref dynamic_states[0]);
//
//             var pipelineInfo = new VkGraphicsPipelineCreateInfo();
//             pipelineInfo.flags = default;
//             pipelineInfo.stageCount = 2;
//             pipelineInfo.pStages = &stage[0];
//             pipelineInfo.pVertexInputState = &vertex_info;
//             pipelineInfo.pInputAssemblyState = &ia_info;
//             pipelineInfo.pViewportState = &viewport_info;
//             pipelineInfo.pRasterizationState = &raster_info;
//             pipelineInfo.pMultisampleState = &ms_info;
//             pipelineInfo.pDepthStencilState = &depth_info;
//             pipelineInfo.pColorBlendState = &blend_info;
//             pipelineInfo.pDynamicState = &dynamic_state;
//             pipelineInfo.layout = _pipelineLayout;
//             pipelineInfo.renderPass = _renderPass;
//             pipelineInfo.subpass = 0;
//             VkPipeline pipeline;
//             if (vkCreateGraphicsPipelines(_device, default, 1, &pipelineInfo, default, &pipeline) != Result.Success)
//             {
//                 throw new Exception("Unable to create the pipeline");
//             }
//             _pipeline = pipeline;
//
//             SilkMarshal.Free((nint)stage[0].pName);
//             SilkMarshal.Free((nint)stage[1].pName);
//
//             // Initialise ImGui Vulkan adapter
//             var io = ImGuiNET.ImGui.GetIO();
//             io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
//             io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height);
//             ulong upload_size = (ulong)(width * height * 4 * sizeof(byte));
//
//             // Submit one-time command to create the fonts texture
//             var poolInfo = new VkCommandPoolCreateInfo();
//             poolInfo.queueFamilyIndex = graphicsFamilyIndex;
//             if (vkCreateCommandPool(_device, &poolInfo, null, out var commandPool) != Result.Success)
//             {
//                 throw new Exception("failed to create command pool!");
//             }
//
//             var allocInfo = new VkCommandBufferAllocateInfo();
//             allocInfo.commandPool = commandPool;
//             allocInfo.level = VkCommandBufferLevel.Primary;
//             allocInfo.commandBufferCount = 1;
//             VkCommandBuffer commandBuffer;
//             if (vkAllocateCommandBuffers(_device, &allocInfo, &commandBuffer) != Result.Success)
//             {
//                 throw new Exception("Unable to allocate command buffers");
//             }
//
//             var beginInfo = new VkCommandBufferBeginInfo();
//             beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
//             if (vkBeginCommandBuffer(commandBuffer, &beginInfo) != Result.Success)
//             {
//                 throw new Exception("Failed to begin a command buffer");
//             }
//
//             var imageInfo = new VkImageCreateInfo();
//             imageInfo.imageType = VkImageType.Image2D;
//             imageInfo.format = VkFormat.R8G8B8A8Unorm;
//             imageInfo.extent.width = (uint)width;
//             imageInfo.extent.height = (uint)height;
//             imageInfo.extent.depth = 1;
//             imageInfo.mipLevels = 1;
//             imageInfo.arrayLayers = 1;
//             imageInfo.samples = VkSampleCountFlags.Count1;
//             imageInfo.tiling = VkImageTiling.Optimal;
//             imageInfo.usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst;
//             imageInfo.sharingMode = VkSharingMode.Exclusive;
//             imageInfo.initialLayout = VkImageLayout.Undefined;
//             if (vkCreateImage(_device, &imageInfo, null, out _fontImage) != Result.Success)
//             {
//                 throw new Exception("Failed to create font image");
//             }
//             vkGetImageMemoryRequirements(_device, _fontImage, out var fontReq);
//             var fontAllocInfo = new VkMemoryAllocateInfo();
//             fontAllocInfo.allocationSize = fontReq.size;
//             fontAllocInfo.memoryTypeIndex = GetMemoryTypeIndex(VkMemoryPropertyFlags.DeviceLocal, fontReq.memoryTypeBits);
//             if (vkAllocateMemory(_device, &fontAllocInfo, null, out _fontMemory) != Result.Success)
//             {
//                 throw new Exception("Failed to allocate device memory");
//             }
//             if (vkBindImageMemory(_device, _fontImage, _fontMemory, 0) != Result.Success)
//             {
//                 throw new Exception("Failed to bind device memory");
//             }
//
//             var imageViewInfo = new VkImageViewCreateInfo();
//             imageViewInfo.image = _fontImage;
//             imageViewInfo.viewType = VkImageViewType.Image2D;
//             imageViewInfo.format = VkFormat.R8G8B8A8Unorm;
//             imageViewInfo.subresourceRange.aspectMask = VkImageAspectFlags.Color;
//             imageViewInfo.subresourceRange.levelCount = 1;
//             imageViewInfo.subresourceRange.layerCount = 1;
//             if (vkCreateImageView(_device, &imageViewInfo, null, out _fontView) != Result.Success)
//             {
//                 throw new Exception("Failed to create an image view");
//             }
//
//             var descImageInfo = new VkDescriptorImageInfo();
//             descImageInfo.sampler = _fontSampler;
//             descImageInfo.imageView = _fontView;
//             descImageInfo.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
//             var writeDescriptors = new VkWriteDescriptorSet();
//             writeDescriptors.dstSet = _descriptorSet;
//             writeDescriptors.descriptorCount = 1;
//             writeDescriptors.descriptorType = VkDescriptorType.CombinedImageSampler;
//             writeDescriptors.pImageInfo = &descImageInfo;
//             vkUpdateDescriptorSets(_device, 1, &writeDescriptors, 0, null);
//
//             // Create the Upload Buffer:
//             var bufferInfo = new VkBufferCreateInfo();
//             bufferInfo.size = upload_size;
//             bufferInfo.usage = VkBufferUsageFlags.TransferSrc;
//             bufferInfo.sharingMode = VkSharingMode.Exclusive;
//             if (vkCreateBuffer(_device, &bufferInfo, default, out var uploadBuffer) != Result.Success)
//             {
//                 throw new Exception("Failed to create a device buffer");
//             }
//
//             vkGetBufferMemoryRequirements(_device, uploadBuffer, out var uploadReq);
//             _bufferMemoryAlignment = (_bufferMemoryAlignment > uploadReq.alignment) ? _bufferMemoryAlignment : uploadReq.alignment;
//
//             var uploadAllocInfo = new VkMemoryAllocateInfo();
//             uploadAllocInfo.allocationSize = uploadReq.size;
//             uploadAllocInfo.memoryTypeIndex = GetMemoryTypeIndex(VkMemoryPropertyFlags.HostVisible, uploadReq.memoryTypeBits);
//             if (vkAllocateMemory(_device, &uploadAllocInfo, null, out var uploadBufferMemory) != Result.Success)
//             {
//                 throw new Exception("Failed to allocate device memory");
//             }
//             if (vkBindBufferMemory(_device, uploadBuffer, uploadBufferMemory, 0) != Result.Success)
//             {
//                 throw new Exception("Failed to bind device memory");
//             }
//
//             void* map = null;
//             if (vkMapMemory(_device, uploadBufferMemory, 0, upload_size, 0, &map) != Result.Success)
//             {
//                 throw new Exception("Failed to map device memory");
//             }
//             Unsafe.CopyBlock(map, pixels.ToPointer(), (uint)upload_size);
//
//             var range = new VkMappedMemoryRange();
//             range.memory = uploadBufferMemory;
//             range.size = upload_size;
//             if (vkFlushMappedMemoryRanges(_device, 1, &range) != Result.Success)
//             {
//                 throw new Exception("Failed to flush memory to device");
//             }
//             vkUnmapMemory(_device, uploadBufferMemory);
//
//             const uint VK_QUEUE_FAMILY_IGNORED = ~0U;
//
//             var copyBarrier = new VkImageMemoryBarrier();
//             copyBarrier.dstAccessMask = VkAccessFlags.TransferWrite;
//             copyBarrier.oldLayout = VkImageLayout.Undefined;
//             copyBarrier.newLayout = VkImageLayout.TransferDstOptimal;
//             copyBarrier.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
//             copyBarrier.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
//             copyBarrier.image = _fontImage;
//             copyBarrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
//             copyBarrier.subresourceRange.levelCount = 1;
//             copyBarrier.subresourceRange.layerCount = 1;
//             vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, 0, 0, null, 0, null, 1, &copyBarrier);
//
//             var region = new VkBufferImageCopy();
//             region.imageSubresource.aspectMask = VkImageAspectFlags.Color;
//             region.imageSubresource.layerCount = 1;
//             region.imageExtent.width = (uint)width;
//             region.imageExtent.height = (uint)height;
//             region.imageExtent.depth = 1;
//             vkCmdCopyBufferToImage(commandBuffer, uploadBuffer, _fontImage, VkImageLayout.TransferDstOptimal, 1, &region);
//
//             var use_barrier = new VkImageMemoryBarrier();
//             use_barrier.srcAccessMask = VkAccessFlags.TransferWrite;
//             use_barrier.dstAccessMask = VkAccessFlags.ShaderRead;
//             use_barrier.oldLayout = VkImageLayout.TransferDstOptimal;
//             use_barrier.newLayout = VkImageLayout.ShaderReadOnlyOptimal;
//             use_barrier.srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
//             use_barrier.dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
//             use_barrier.image = _fontImage;
//             use_barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
//             use_barrier.subresourceRange.levelCount = 1;
//             use_barrier.subresourceRange.layerCount = 1;
//             vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.FragmentShader, 0, 0, null, 0, null, 1, &use_barrier);
//
//             // Store our identifier
//             io.Fonts.SetTexID((IntPtr)_fontImage.Handle);
//
//             if (vkEndCommandBuffer(commandBuffer) != Result.Success)
//             {
//                 throw new Exception("Failed to begin a command buffer");
//             }
//
//             vkGetDeviceQueue(_device, graphicsFamilyIndex, 0, out var graphicsQueue);
//
//             var submitInfo = new VkSubmitInfo();
//             submitInfo.commandBufferCount = 1;
//             submitInfo.pCommandBuffers = (VkCommandBuffer*)Unsafe.AsPointer(ref commandBuffer);
//             if (vkQueueSubmit(graphicsQueue, 1, &submitInfo, default) != Result.Success)
//             {
//                 throw new Exception("Failed to begin a command buffer");
//             }
//
//             if (vkQueueWaitIdle(graphicsQueue) != Result.Success)
//             {
//                 throw new Exception("Failed to begin a command buffer");
//             }
//
//             vkDestroyBuffer(_device, uploadBuffer);
//             vkFreeMemory(_device, uploadBufferMemory);
//             vkDestroyCommandPool(_device, commandPool);
//         }
//
//         private uint GetMemoryTypeIndex(VkMemoryPropertyFlags properties, uint type_bits)
//         {
//             vkGetPhysicalDeviceMemoryProperties(_physicalDevice, out var prop);
//             for (int i = 0; i < prop.memoryTypeCount; i++)
//             {
//                 if (((prop.memoryTypes)[i].propertyFlags & properties) == properties && (type_bits & (1u << i)) != 0)
//                 {
//                     return (uint)i;
//                 }
//             }
//             return 0xFFFFFFFF; // Unable to find memoryType
//         }
//
//         private void BeginFrame()
//         {
//             ImGuiNET.ImGui.NewFrame();
//             _frameBegun = true;
//             _keyboard = _input.Keyboards[0];
//             _view.Resize += WindowResized;
//             _keyboard.KeyChar += OnKeyChar;
//         }
//
//         private void OnKeyChar(IKeyboard arg1, char arg2)
//         {
//             _pressedChars.Add(arg2);
//         }
//
//         private void WindowResized(Vector2D<int> size)
//         {
//             _windowWidth = size.X;
//             _windowHeight = size.Y;
//         }
//
//         /// <summary>
//         /// Renders the ImGui draw list data.
//         /// </summary>
//         public void Render(VkCommandBuffer commandBuffer, VkFramebuffer framebuffer, VkExtent2D swapChainExtent)
//         {
//             if (_frameBegun)
//             {
//                 _frameBegun = false;
//                 ImGuiNET.ImGui.Render();
//                 RenderImDrawData(ImGuiNET.ImGui.GetDrawData(), commandBuffer, framebuffer, swapChainExtent);
//             }
//         }
//
//         /// <summary>
//         /// Updates ImGui input and IO configuration state. Call Update() before drawing and rendering.
//         /// </summary>
//         public void Update(float deltaSeconds)
//         {
//             if (_frameBegun)
//             {
//                 ImGuiNET.ImGui.Render();
//             }
//
//             SetPerFrameImGuiData(deltaSeconds);
//             UpdateImGuiInput();
//
//             _frameBegun = true;
//             ImGuiNET.ImGui.NewFrame();
//         }
//
//         private void SetPerFrameImGuiData(float deltaSeconds)
//         {
//             var io = ImGuiNET.ImGui.GetIO();
//             io.DisplaySize = new Vector2(_windowWidth, _windowHeight);
//
//             if (_windowWidth > 0 && _windowHeight > 0)
//             {
//                 io.DisplayFramebufferScale = new Vector2(_view.FramebufferSize.X / _windowWidth, _view.FramebufferSize.Y / _windowHeight);
//             }
//
//             io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
//         }
//
//         private void UpdateImGuiInput()
//         {
//             return;
//             var io = ImGuiNET.ImGui.GetIO();
//
//             var mouseState = _input.Mice[0].CaptureState();
//             var keyboardState = _input.Keyboards[0];
//
//             io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
//             io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
//             io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);
//
//             var point = new Point((int)mouseState.Position.X, (int)mouseState.Position.Y);
//             io.MousePos = new Vector2(point.X, point.Y);
//
//             var wheel = mouseState.GetScrollWheels()[0];
//             io.MouseWheel = wheel.Y;
//             io.MouseWheelH = wheel.X;
//
//             foreach (var key in KeyValues)
//             {
//                 if (key == Key.Unknown)
//                 {
//                     continue;
//                 }
//                 io.KeysDown[(int)key] = keyboardState.IsKeyPressed(key);
//             }
//
//             foreach (var c in _pressedChars)
//             {
//                 io.AddInputCharacter(c);
//             }
//
//             _pressedChars.Clear();
//
//             io.KeyCtrl = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
//             io.KeyAlt = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);
//             io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
//             io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft) || keyboardState.IsKeyPressed(Key.SuperRight);
//         }
//
//         internal void PressChar(char keyChar)
//         {
//             _pressedChars.Add(keyChar);
//         }
//
//         private static void SetKeyMappings()
//         {
//             var io = ImGuiNET.ImGui.GetIO();
//             io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
//             io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
//             io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
//             io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
//             io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
//             io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
//             io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
//             io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
//             io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
//             io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
//             io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Backspace;
//             io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
//             io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
//             io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
//             io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
//             io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
//             io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
//             io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
//             io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
//         }
//
//         private unsafe void RenderImDrawData(in ImDrawDataPtr drawDataPtr, in VkCommandBuffer commandBuffer, in VkFramebuffer framebuffer, in VkExtent2D swapChainExtent)
//         {
//             int framebufferWidth = (int)(drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X);
//             int framebufferHeight = (int)(drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y);
//             if (framebufferWidth <= 0 || framebufferHeight <= 0)
//             {
//                 return;
//             }
//
//             // var renderPassInfo = new RenderPassBeginInfo();
//             // renderPassInfo.SType = StructureType.RenderPassBeginInfo;
//             // renderPassInfo.RenderPass = _renderPass;
//             // renderPassInfo.Framebuffer = framebuffer;
//             // renderPassInfo.RenderArea.offset = default;
//             // renderPassInfo.RenderArea.Extent = swapChainExtent;
//             // renderPassInfo.ClearValueCount = 0;
//             // renderPassInfo.PClearValues = default;
//             //
//             // vkCmdBeginRenderPass(commandBuffer, &renderPassInfo, SubpassContents.Inline);
//
//             var drawData = *drawDataPtr.NativePtr;
//
//             // Avoid rendering when minimized, scale coordinates for retina displays (screen coordinates != framebuffer coordinates)
//             int fb_width = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
//             int fb_height = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
//             if (fb_width <= 0 || fb_height <= 0)
//             {
//                 return;
//             }
//
//             // Allocate array to store enough vertex/index buffers
//             if (_mainWindowRenderBuffers.FrameRenderBuffers == null)
//             {
//                 _mainWindowRenderBuffers.Index = 0;
//                 _mainWindowRenderBuffers.Count = (uint)_swapChainImageCt;
//                 _frameRenderBuffers = GlobalMemory.Allocate(sizeof(FrameRenderBuffer) * (int)_mainWindowRenderBuffers.Count);
//                 _mainWindowRenderBuffers.FrameRenderBuffers = _frameRenderBuffers.AsPtr<FrameRenderBuffer>();
//                 for (int i = 0; i < (int)_mainWindowRenderBuffers.Count; i++)
//                 {
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].IndexBuffer = 0;
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].IndexBufferSize = 0;
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].IndexBufferMemory = 0;
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].VertexBuffer = 0;
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].VertexBufferSize = 0;
//                     _mainWindowRenderBuffers.FrameRenderBuffers[i].VertexBufferMemory = 0;
//                 }
//             }
//             _mainWindowRenderBuffers.Index = (_mainWindowRenderBuffers.Index + 1) % _mainWindowRenderBuffers.Count;
//
//             ref FrameRenderBuffer frameRenderBuffer = ref _mainWindowRenderBuffers.FrameRenderBuffers[_mainWindowRenderBuffers.Index];
//
//             if (drawData.TotalVtxCount > 0)
//             {
//                 // Create or resize the vertex/index buffers
//                 ulong vertex_size = (ulong)drawData.TotalVtxCount * (ulong)sizeof(ImDrawVert);
//                 ulong index_size = (ulong)drawData.TotalIdxCount * (ulong)sizeof(ushort);
//                 if (frameRenderBuffer.VertexBuffer.Handle == default || frameRenderBuffer.VertexBufferSize < vertex_size)
//                 {
//                     CreateOrResizeBuffer(ref frameRenderBuffer.VertexBuffer, ref frameRenderBuffer.VertexBufferMemory, ref frameRenderBuffer.VertexBufferSize, vertex_size, VkBufferUsageFlags.VertexBuffer);
//                 }
//                 if (frameRenderBuffer.IndexBuffer.Handle == default || frameRenderBuffer.IndexBufferSize < index_size)
//                 {
//                     CreateOrResizeBuffer(ref frameRenderBuffer.IndexBuffer, ref frameRenderBuffer.IndexBufferMemory, ref frameRenderBuffer.IndexBufferSize, index_size, VkBufferUsageFlags.IndexBuffer);
//                 }
//
//                 // Upload vertex/index data into a single contiguous GPU buffer
//                 ImDrawVert* vtx_dst = null;
//                 ushort* idx_dst = null;
//                 if (vkMapMemory(_device, frameRenderBuffer.VertexBufferMemory, 0, frameRenderBuffer.VertexBufferSize, 0, (void**)(&vtx_dst)) != Result.Success)
//                 {
//                     throw new Exception($"Unable to map device memory");
//                 }
//                 if (vkMapMemory(_device, frameRenderBuffer.IndexBufferMemory, 0, frameRenderBuffer.IndexBufferSize, 0, (void**)(&idx_dst)) != Result.Success)
//                 {
//                     throw new Exception($"Unable to map device memory");
//                 }
//                 for (int n = 0; n < drawData.CmdListsCount; n++)
//                 {
//                     ImDrawList* cmd_list = drawDataPtr.CmdLists[n];
//                     Unsafe.CopyBlock(vtx_dst, cmd_list->VtxBuffer.Data.ToPointer(), (uint)cmd_list->VtxBuffer.Size * (uint)sizeof(ImDrawVert));
//                     Unsafe.CopyBlock(idx_dst, cmd_list->IdxBuffer.Data.ToPointer(), (uint)cmd_list->IdxBuffer.Size * (uint)sizeof(ushort));
//                     vtx_dst += cmd_list->VtxBuffer.Size;
//                     idx_dst += cmd_list->IdxBuffer.Size;
//                 }
//
//                 var range = stackalloc VkMappedMemoryRange[2];
//                 range[0] = new();
//                 range[0].memory = frameRenderBuffer.VertexBufferMemory;
//                 range[0].size = VK_WHOLE_SIZE;
//                 range[1] = new();
//                 range[1].memory = frameRenderBuffer.IndexBufferMemory;
//                 range[1].size = VK_WHOLE_SIZE;
//                 if (vkFlushMappedMemoryRanges(_device, 2, range) != Result.Success)
//                 {
//                     throw new Exception($"Unable to flush memory to device");
//                 }
//                 vkUnmapMemory(_device, frameRenderBuffer.VertexBufferMemory);
//                 vkUnmapMemory(_device, frameRenderBuffer.IndexBufferMemory);
//             }
//
//             // Setup desired Vulkan state
//             vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, _pipeline);
//             VkDescriptorSet* descriptor_sets = stackalloc VkDescriptorSet[1] { _descriptorSet };
//             vkCmdBindDescriptorSets(commandBuffer, VkPipelineBindPoint.Graphics, _pipelineLayout, 0, 1, descriptor_sets, 0, null);
//
//             // Bind Vertex And Index Buffer:
//             if (drawData.TotalVtxCount > 0)
//             {
//                 var vertex_buffers = stackalloc VkBuffer[] { frameRenderBuffer.VertexBuffer };
//                 ulong vertex_offset = 0;
//                 vkCmdBindVertexBuffers(commandBuffer, 0, 1, vertex_buffers, (ulong*)Unsafe.AsPointer(ref vertex_offset));
//                 vkCmdBindIndexBuffer(commandBuffer, frameRenderBuffer.IndexBuffer, 0, sizeof(ushort) == 2 ? VkIndexType.Uint16 : VkIndexType.Uint32);
//             }
//
//             // Setup viewport:
//             VkViewport viewport;
//             viewport.x = 0;
//             viewport.y = 0;
//             viewport.width = (float)fb_width;
//             viewport.height = (float)fb_height;
//             viewport.minDepth = 0.0f;
//             viewport.maxDepth = 1.0f;
//             vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
//
//             // Setup scale and translation:
//             // Our visible imgui space lies from draw_data.DisplayPps (top left) to draw_data.DisplayPos+data_data.DisplaySize (bottom right). DisplayPos is (0,0) for single viewport apps.
//             var scale = stackalloc float[2];
//             scale[0] = 2.0f / drawData.DisplaySize.X;
//             scale[1] = 2.0f / drawData.DisplaySize.Y;
//             var translate = stackalloc float[2];
//             translate[0] = -1.0f - drawData.DisplayPos.X * scale[0];
//             translate[1] = -1.0f - drawData.DisplayPos.Y * scale[1];
//             vkCmdPushConstants(commandBuffer, _pipelineLayout, VkShaderStageFlags.Vertex, sizeof(float) * 0, sizeof(float) * 2, scale);
//             vkCmdPushConstants(commandBuffer, _pipelineLayout, VkShaderStageFlags.Vertex, sizeof(float) * 2, sizeof(float) * 2, translate);
//
//             // Will project scissor/clipping rectangles into framebuffer space
//             Vector2 clipOff = drawData.DisplayPos;         // (0,0) unless using multi-viewports
//             Vector2 clipScale = drawData.FramebufferScale; // (1,1) unless using retina display which are often (2,2)
//
//             // Render command lists
//             // (Because we merged all buffers into a single one, we maintain our own offset into them)
//             int vertexOffset = 0;
//             int indexOffset = 0;
//             for (int n = 0; n < drawData.CmdListsCount; n++)
//             {
//                 ImDrawList* cmd_list = drawDataPtr.CmdLists[n];
//                 for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
//                 {
//                     ref ImDrawCmd pcmd = ref cmd_list->CmdBuffer.Ref<ImDrawCmd>(cmd_i);
//
//                     // Project scissor/clipping rectangles into framebuffer space
//                     Vector4 clipRect;
//                     clipRect.X = (pcmd.ClipRect.X - clipOff.X) * clipScale.X;
//                     clipRect.Y = (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y;
//                     clipRect.Z = (pcmd.ClipRect.Z - clipOff.X) * clipScale.X;
//                     clipRect.W = (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y;
//
//                     if (clipRect.X < fb_width && clipRect.Y < fb_height && clipRect.Z >= 0.0f && clipRect.W >= 0.0f)
//                     {
//                         // Negative offsets are illegal for vkCmdSetScissor
//                         if (clipRect.X < 0.0f)
//                             clipRect.X = 0.0f;
//                         if (clipRect.Y < 0.0f)
//                             clipRect.Y = 0.0f;
//
//                         // Apply scissor/clipping rectangle
//                         VkRect2D scissor = new VkRect2D();
//                         scissor.offset.x = (int)clipRect.X;
//                         scissor.offset.y = (int)clipRect.Y;
//                         scissor.extent.width = (uint)(clipRect.Z - clipRect.X);
//                         scissor.extent.height = (uint)(clipRect.W - clipRect.Y);
//                         vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
//
//                         // Draw
//                         vkCmdDrawIndexed(commandBuffer, pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)indexOffset, (int)pcmd.VtxOffset + vertexOffset, 0);
//                     }
//                 }
//                 indexOffset += cmd_list->IdxBuffer.Size;
//                 vertexOffset += cmd_list->VtxBuffer.Size;
//             }
//
//             // vkCmdEndRenderPass(commandBuffer);
//         }
//
//         unsafe void CreateOrResizeBuffer(ref VkBuffer buffer, ref VkDeviceMemory buffer_memory, ref ulong bufferSize, ulong newSize, VkBufferUsageFlags usage)
//         {
//             if (buffer.Handle != default)
//             {
//                 vkDestroyBuffer(_device, buffer, null);
//             }
//             if (buffer_memory.Handle != default)
//             {
//                 vkFreeMemory(_device, buffer_memory, null);
//             }
//
//             ulong sizeAlignedVertexBuffer = ((newSize - 1) / _bufferMemoryAlignment + 1) * _bufferMemoryAlignment;
//             var bufferInfo = new VkBufferCreateInfo();
//             bufferInfo.size = sizeAlignedVertexBuffer;
//             bufferInfo.usage = usage;
//             bufferInfo.sharingMode = VkSharingMode.Exclusive;
//             if (vkCreateBuffer(_device, &bufferInfo, null, out buffer) != Result.Success)
//             {
//                 throw new Exception($"Unable to create a device buffer");
//             }
//
//             vkGetBufferMemoryRequirements(_device, buffer, out var req);
//             _bufferMemoryAlignment = (_bufferMemoryAlignment > req.alignment) ? _bufferMemoryAlignment : req.alignment;
//             var allocInfo = new VkMemoryAllocateInfo();
//             allocInfo.allocationSize = req.size;
//             allocInfo.memoryTypeIndex = GetMemoryTypeIndex(VkMemoryPropertyFlags.HostVisible, req.memoryTypeBits);
//             if (vkAllocateMemory(_device, &allocInfo, null, out buffer_memory) != Result.Success)
//             {
//                 throw new Exception($"Unable to allocate device memory");
//             }
//
//             if (vkBindBufferMemory(_device, buffer, buffer_memory, 0) != Result.Success)
//             {
//                 throw new Exception($"Unable to bind device memory");
//             }
//             bufferSize = req.size;
//         }
//
//         /// <summary>
//         /// Frees all graphics resources used by the renderer.
//         /// </summary>
//         public unsafe void Dispose()
//         {
//             _view.Resize -= WindowResized;
//             _keyboard.KeyChar -= OnKeyChar;
//
//             for (uint n = 0; n < _mainWindowRenderBuffers.Count; n++)
//             {
//                 vkDestroyBuffer(_device, _mainWindowRenderBuffers.FrameRenderBuffers[n].VertexBuffer, default);
//                 vkFreeMemory(_device, _mainWindowRenderBuffers.FrameRenderBuffers[n].VertexBufferMemory, default);
//                 vkDestroyBuffer(_device, _mainWindowRenderBuffers.FrameRenderBuffers[n].IndexBuffer, default);
//                 vkFreeMemory(_device, _mainWindowRenderBuffers.FrameRenderBuffers[n].IndexBufferMemory, default);
//             }
//
//             vkDestroyShaderModule(_device, _shaderModuleVert, default);
//             vkDestroyShaderModule(_device, _shaderModuleFrag, default);
//             vkDestroyImageView(_device, _fontView, default);
//             vkDestroyImage(_device, _fontImage, default);
//             vkFreeMemory(_device, _fontMemory, default);
//             vkDestroySampler(_device, _fontSampler, default);
//             vkDestroyDescriptorSetLayout(_device, _descriptorSetLayout, default);
//             vkDestroyPipelineLayout(_device, _pipelineLayout, default);
//             vkDestroyPipeline(_device, _pipeline, default);
//             vkDestroyDescriptorPool(_device, _descriptorPool, default);
//             // vkDestroyRenderPass(vkCurrentDevice.Value, _renderPass, default);
//
//             ImGuiNET.ImGui.DestroyContext();
//         }
//
//         struct FrameRenderBuffer
//         {
//             public VkDeviceMemory VertexBufferMemory;
//             public VkDeviceMemory IndexBufferMemory;
//             public ulong VertexBufferSize;
//             public ulong IndexBufferSize;
//             public VkBuffer VertexBuffer;
//             public VkBuffer IndexBuffer;
//         };
//
//         unsafe struct WindowRenderBuffers
//         {
//             public uint Index;
//             public uint Count;
//             public FrameRenderBuffer* FrameRenderBuffers;
//         };
//     }
// }
