using System.Runtime.InteropServices;
using Vortice.Vulkan;
using VulkanEngine.Renderer;
using VulkanEngine.Renderer.ECS;
using VulkanEngine.Renderer.GPUStructs;

namespace VulkanEngine.Phases.FrameRender;

public static class WriteoutRenderObjects
{
    public static int writenObjectCount = 0;
    public static void WriteOutObjectData()
    {
        var query = MakeQuery<Transform_ref, MeshComponent>();
        var count = 0;
        VKRender.EnsureRenderObjectRelatedBuffersAreSized(MeshComponent._data.used);// overkill if we have meshes without transforms, but why would we???
        
        while (HasResults(ref query, out var id, out var transform, out var meshComponent))
        {
            VKRender.GetCurrentFrame().hostRenderObjectsBufferAsSpan[count++] = new ()
            {
                transform = transform.local_to_world_matrix,
                meshID = (uint) meshComponent.Mesh.index,
                materialID = 0,
            };
        }
        Volatile.Write(ref writenObjectCount,count);

    }

    private static readonly unsafe VkDescriptorBufferInfo* bufInfos =
        (VkDescriptorBufferInfo*) NativeMemory.Alloc((nuint) (Marshal.SizeOf<VkDescriptorBufferInfo>() * 3));
    private static readonly unsafe VkWriteDescriptorSet* writes =
        (VkWriteDescriptorSet*) NativeMemory.Alloc((nuint) (Marshal.SizeOf<VkWriteDescriptorSet>() * 3));
    public static unsafe Span<VkWriteDescriptorSet> Descriptors(uint baseBindNo)
    {
        ulong inRange = (ulong) VKRender.GlobalData.deviceRenderObjectsBufferSizeInBytes;
        ulong outRange = (ulong) VKRender.GlobalData.deviceIndirectDrawBufferSizeInBytes;
            
        bufInfos[0] = new VkDescriptorBufferInfo
        {
            buffer = VKRender.GlobalData.deviceRenderObjectsBuffer,
            offset = 0,
            range = inRange, //todo update live
        };
        bufInfos[1] = new VkDescriptorBufferInfo
        {
            buffer = VKRender.GlobalData.deviceIndirectDrawBuffer,
            offset = 0,
            range = outRange, //todo update live
        };

        bufInfos[2] = new VkDescriptorBufferInfo
        {
            buffer = VKRender.GlobalData.MeshInfoBuffer,
            offset = 0,
            range = (ulong) (VKRender.GlobalData.MeshInfoBufferSize * sizeof(Renderer.GPUStructs.MeshInfo)) ,
        };
        // var OutputBufferForGfx = new VkDescriptorBufferInfo
        // {
        //     buffer = VKRender.GlobalData.deviceIndirectDrawBuffer,
        //     offset = 0,
        //     range = outRange,
        // };


        writes[0] = new()
        {
            dstSet = default,
            dstBinding = baseBindNo,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.StorageBuffer,
            descriptorCount = 1,
            pBufferInfo = &bufInfos[0],
        };
        writes[1] = new()
        {
            dstSet = default,
            dstBinding = baseBindNo+1,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.StorageBuffer,
            descriptorCount = 1,
            pBufferInfo = &bufInfos[1],
        };
        writes[2] = new()
        {
            dstSet = default,
            dstBinding = baseBindNo+2,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.StorageBuffer,
            descriptorCount = 1,
            pBufferInfo = &bufInfos[2],
        };

        return new (writes, 3);


    }


    
}