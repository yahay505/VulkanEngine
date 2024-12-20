// using Vortice.Vulkan;
// using VulkanEngine.Phases.FrameRender;
// using VulkanEngine.Renderer;
// using VulkanEngine.Renderer2.infra.Bindless;
// using static Vortice.Vulkan.Vulkan;
// using VKRender = VulkanEngine.Renderer2.infra.Config.VKRender;
//
// namespace VulkanEngine;
//
// public class RenderPath
// {
//     public static unsafe void  Render()
//     {
//         uint maxMeshCount=(uint)Volatile.Read(ref WriteoutRenderObjects.writenObjectCount);
//
//         // get commandBuffer
//         var cb = VKRender.GetRenderCB1();
//         // start cb
//         vkBeginCommandBuffer(cb, VkCommandBufferUsageFlags.OneTimeSubmit);
//         // update CS bufs
//         Span<VkWriteDescriptorSet> CompDescWrites = [
//             ..WriteoutRenderObjects.Descriptors(1),
//             MaterialManager.Bind(4)
//         ];
//         // bind CS layout
//         vkCmdBindPipeline(cb,VK_PIPELINE_BIND_POINT_COMPUTE,VKRender.ComputePipeline);
//         // push descriptors
//         vkCmdPushDescriptorSetKHR(cb,VK_PIPELINE_BIND_POINT_COMPUTE,VKRender.ComputePipelineLayout,0,(uint) CompDescWrites.Length,CompDescWrites.ptr());
//         // exec 
//         vkCmdDispatch(cb,1,1,1);
//         // update GFX bufs
//         // barrier
//         
//         // starts gfx
//         vkCmdBindPipeline(cb,VkPipelineBindPoint.Graphics,VKRender.GraphicsPipeline);
//         // push descrptors
//         Span<VkWriteDescriptorSet> gfxDesc0Writes = [
//             
//         ];
//         Span<VkDescriptorSet> bindlessSets = [
//             TextureManager.descriptorset
//         ];
//         vkCmdPushDescriptorSetKHR(cb,VkPipelineBindPoint.Graphics,VKRender.GfxPipelineLayout,0,(uint) gfxDesc0Writes.Length,gfxDesc0Writes.ptr());
//         vkCmdBindDescriptorSets(cb,VkPipelineBindPoint.Graphics,VKRender.GfxPipelineLayout,1,bindlessSets);
//         // Draw
//         vkCmdDrawIndexedIndirectCount(cb,VKRender.GlobalData.deviceIndirectDrawBuffer,VKRender.ComputeOutSSBOStartOffset,VKRender.GlobalData.deviceIndirectDrawBuffer,0,maxMeshCount,(uint) sizeof(Renderer.GPUStructs.ComputeDrawOutput));
//         // stop CB
//         // present
//
//     }
// }