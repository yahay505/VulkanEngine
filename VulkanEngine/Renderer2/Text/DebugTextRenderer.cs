// using Vortice.Vulkan;
// using VulkanEngine.Renderer2.infra.Bindless;
// using static Vortice.Vulkan.Vulkan;
//
// namespace VulkanEngine.Renderer2.Text;
//
// public static class DebugTextRenderer
// {
//     private static int ent1;
//     private static Renderer2.infra.Config.VKRender.GPUDynamicBuffer<float2> VertexBuffer;
//     private static Renderer2.infra.Config.VKRender.GPUDynamicBuffer<float2> UVBuffer;
//     private static Renderer2.infra.Config.VKRender.GPUDynamicBuffer<uint> IndexBuffer;
//     private static VkPipeline pipeline;
//     private static uint binding;
//
//     public static unsafe void init()
//     {
//         ent1 = CreateEntity();
//         var trans = TransformSystem.AddItemWithGlobalID(ent1);
//         trans.local_scale = new float3(10);
//         
//         VertexBuffer= new Renderer2.infra.Config.VKRender.GPUDynamicBuffer<float2>(4,VkBufferUsageFlags.VertexBuffer,VkMemoryPropertyFlags.HostVisible,"DebugTextRenderer VertexBuffer"u8);
//         UVBuffer= new Renderer2.infra.Config.VKRender.GPUDynamicBuffer<float2>(4,VkBufferUsageFlags.VertexBuffer,VkMemoryPropertyFlags.HostVisible,"DebugTextRenderer UVBuffer"u8);
//         IndexBuffer= new Renderer2.infra.Config.VKRender.GPUDynamicBuffer<uint>(6,VkBufferUsageFlags.IndexBuffer,VkMemoryPropertyFlags.HostVisible,"DebugTextRenderer IndexBuffer"u8);
//         var Vptr = VertexBuffer.MapOrGetAdress();
//         var UVptr = UVBuffer.MapOrGetAdress();
//         var Iptr = IndexBuffer.MapOrGetAdress();
//         Vptr[0] = new(-1,1);
//         Vptr[1] = new(1,1);
//         Vptr[2] = new(1,-1);
//         Vptr[3] = new(-1,-1);
//         UVptr[0] = new(0,0);
//         UVptr[1] = new(1,0);
//         UVptr[2] = new(1,1);
//         UVptr[3] = new(0,1);
//         Iptr[0] = 0;
//         Iptr[1] = 1;
//         Iptr[2] = 2;
//         Iptr[3] = 0;
//         Iptr[4] = 2;
//         Iptr[5] = 3;
//         
//         // load image
//         var image = TextureManager.CreateImage(Renderer2.infra.Config.VKRender.AssetsPath + "textures/Font.png");
//         TextureManager.LoadImage(image);
//         var cb = Renderer2.infra.Config.VKRender.BeginSingleTimeCommands();
//         TextureManager.UploadImage(cb,image,out var cleanup);
//         Renderer2.infra.Config.VKRender.EndSingleTimeCommands(cb);
//         TextureManager.CrateView(image);
//         cleanup();
//         binding = TextureManager.Bind(image);
//
//         // pso
//         var vertexShader = Renderer2.infra.Config.VKRender.CreateShaderModule(File.ReadAllBytes(Renderer2.infra.Config.VKRender.AssetsPath+"shaders/compiled/text.vert.spv"));
//         var fragmentShader = Renderer2.infra.Config.VKRender.CreateShaderModule(File.ReadAllBytes(Renderer2.infra.Config.VKRender.AssetsPath+"shaders/compiled/text.frag.spv"));
//         var fnName = "main"u8;
//         
//         
//         var colorBlendAttachment = new VkPipelineColorBlendAttachmentState()
//         {
//             blendEnable = true,
//             alphaBlendOp = VkBlendOp.Add,
//             colorBlendOp = VkBlendOp.Add,
//             colorWriteMask = VkColorComponentFlags.All,
//             srcAlphaBlendFactor = VkBlendFactor.One,
//             dstAlphaBlendFactor = VkBlendFactor.Zero,
//             srcColorBlendFactor = VkBlendFactor.SrcAlpha,
//             dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha
//         };
//         pipeline = Renderer2.infra.Config.VKRender.CreatePSO(
//             [
//                 new()
//                 {
//                     flags = VkPipelineShaderStageCreateFlags.None,
//                     module = vertexShader,
//                     pName = (sbyte*) fnName.GetPointer(),
//                     stage = VkShaderStageFlags.Vertex,
//                 },
//                 new()
//                 {
//                     flags = VkPipelineShaderStageCreateFlags.None,
//                     module = fragmentShader,
//                     pName = (sbyte*) fnName.GetPointer(),
//                     stage = VkShaderStageFlags.Fragment,
//                 }
//             ],
//             TextVertexFormat.GetBindingDescription(0),
//             TextVertexFormat.GetAttributeDescriptions(0),
//             VkPrimitiveTopology.TriangleList,
//             false,
//             dynamicStates:[VkDynamicState.Viewport, VkDynamicState.Scissor],
//             new()
//             {
//                 pNext = null,
//                 flags = VkPipelineRasterizationStateCreateFlags.None,
//                 depthClampEnable = false,
//                 rasterizerDiscardEnable = false,
//                 polygonMode = VkPolygonMode.Fill,
//                 cullMode = VkCullModeFlags.Back,
//                 frontFace = VkFrontFace.CounterClockwise,
//                 depthBiasEnable = false,
//                 depthBiasConstantFactor = 0,
//                 depthBiasClamp = 0,
//                 depthBiasSlopeFactor = 0,
//                 lineWidth = 1,
//             },
//             new()
//             {
//                 sampleShadingEnable = false,
//                 rasterizationSamples = VkSampleCountFlags.Count1,
//                 minSampleShading = 1,
//                 pSampleMask = null,
//                 alphaToCoverageEnable = false,
//                 alphaToOneEnable = false,
//             },
//             new()
//             {
//                 depthTestEnable = true,
//                 depthWriteEnable = false,
//                 depthCompareOp = VkCompareOp.Less,
//                 depthBoundsTestEnable = false,
//                 stencilTestEnable = false,
//             },
//             new()
//             {
//                 logicOpEnable = false,
//                 logicOp = VkLogicOp.Copy,
//                 attachmentCount = 1,
//                 pAttachments = &colorBlendAttachment,
//             },
//             Renderer2.infra.Config.VKRender.GfxPipelineLayout,
//             Renderer2.infra.Config.VKRender.RenderPass,
//             0
//         );
//     }
//
//     public static unsafe void Draw(VkCommandBuffer commandBuffer)
//     {
//         vkCmdBindPipeline(commandBuffer,VkPipelineBindPoint.Graphics,pipeline);
//         vkCmdBindIndexBuffer(commandBuffer,IndexBuffer.buffer,0,VkIndexType.Uint32);
//         vkCmdBindVertexBuffers(commandBuffer,0,[VertexBuffer.buffer, UVBuffer.buffer],[0,0]);
//         vkCmdDrawIndexed(commandBuffer,6,1,0,0,0);
//     }
//
//
// }