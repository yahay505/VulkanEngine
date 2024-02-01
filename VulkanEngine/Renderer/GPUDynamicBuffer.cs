// #define GPUDEBG
using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanEngine.Renderer;
public static partial class VKRender{
static class GPUDynamicBuffer
{
    public static Buffer stagingBuffer;
    public static DeviceMemory stagingMemory;
    public static unsafe void* stagingMemoryPtr;
    public static int sizeOfStagingMemory=0;
    
}
public class GPUDynamicBuffer<T> where T:unmanaged
{
    private readonly BufferUsageFlags _usage;
    private readonly MemoryPropertyFlags _properties;
    public Buffer buffer;
    public DeviceMemory memory;
    private int currentSize=0;
    private ulong currentSizeInBytes=>(ulong) (currentSize*Unsafe.SizeOf<T>());
    private uint currentbyteOffset=0;

    public unsafe void* DebugPtr;
    public GPUDynamicBuffer(ulong initialSizeInItems, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        _usage = usage|BufferUsageFlags.TransferDstBit|BufferUsageFlags.TransferSrcBit;
        _properties = properties;
#if GPUDEBG
        _properties &= ~MemoryPropertyFlags.DeviceLocalBit;
        _properties |= MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit;
        
        
#endif
        
        
        currentSize = (int) initialSizeInItems;
    
        CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
        
        unsafe
        {
            fixed (void** debugPtr = &DebugPtr)
                vk.MapMemory(device, memory, 0, currentSizeInBytes, 0,
                    debugPtr);
        }
    }

    public unsafe uint Upload(Span<T> data)
    {
        var cmdBuf=BeginSingleTimeCommands();

        var dataLength = (uint)data.Length * Unsafe.SizeOf<T>();
        if (currentbyteOffset+dataLength > (int) currentSizeInBytes)
        {
            
            var oldbuf=buffer;
            var oldmem=memory;
            var oldbufsizeinbytes=currentSizeInBytes;
            var newSize = Math.Max( data.Length,(currentSize * 2));
            currentSize = newSize;
            CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
            fixed (void** debugPtr = &DebugPtr)
                vk.MapMemory(device, memory, 0, currentSizeInBytes, 0,
                    debugPtr);
            vk.CmdCopyBuffer(cmdBuf, oldbuf, buffer, 1, new BufferCopy()
            {
                Size = oldbufsizeinbytes
            });
            var BufferMemoryBarriers = new BufferMemoryBarrier()
            {
                SType = StructureType.BufferMemoryBarrier,
                Buffer = buffer,
                Size = currentSizeInBytes,
                SrcAccessMask = AccessFlags.TransferWriteBit,
                DstAccessMask = AccessFlags.TransferWriteBit,
                SrcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
                DstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
            };
            vk.CmdPipelineBarrier(cmdBuf,
                PipelineStageFlags.TransferBit,
                PipelineStageFlags.TransferBit,
                0,
                0,
                null,
                1,
                &BufferMemoryBarriers,
                0u,
                null);
            RegisterBufferForCleanup(oldbuf, oldmem);
        }

        if (dataLength > GPUDynamicBuffer.sizeOfStagingMemory)
        {
            CleanupBufferImmediately(GPUDynamicBuffer.stagingBuffer, GPUDynamicBuffer.stagingMemory);
            CreateBuffer((ulong) dataLength,
                BufferUsageFlags.TransferSrcBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                out GPUDynamicBuffer.stagingBuffer,
                out GPUDynamicBuffer.stagingMemory);
            fixed (void** pstagingMemoryPtr = &GPUDynamicBuffer.stagingMemoryPtr)
                vk.MapMemory(device, GPUDynamicBuffer.stagingMemory, 0, (ulong) dataLength, 0,
                pstagingMemoryPtr);
        }
        data.CopyTo(new Span<T>(GPUDynamicBuffer.stagingMemoryPtr,data.Length));
        //memory barrier
        var BufferMemoryBarriers2 = new BufferMemoryBarrier()
        {
            SType = StructureType.BufferMemoryBarrier,

            Buffer = GPUDynamicBuffer.stagingBuffer,
            Size = (ulong) dataLength,
            SrcAccessMask = AccessFlags.HostWriteBit,
            DstAccessMask = AccessFlags.TransferReadBit,
            SrcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
            DstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
        };
        vk.CmdPipelineBarrier(cmdBuf,
            PipelineStageFlags.HostBit,
            PipelineStageFlags.TransferBit,
            0,
            0,
            null,
            1,
            &BufferMemoryBarriers2,
            0u,
            null);
        vk.CmdCopyBuffer(cmdBuf, GPUDynamicBuffer.stagingBuffer, buffer, 1, new BufferCopy()
        {
            Size = (ulong) dataLength,
            SrcOffset = 0,
            DstOffset = (ulong) currentbyteOffset
        });
        //memory barrier
        var BufferMemoryBarriers3 = new BufferMemoryBarrier()
        {
            SType = StructureType.BufferMemoryBarrier,
            Buffer = buffer,
            Size = (ulong) dataLength,
            SrcAccessMask = AccessFlags.TransferWriteBit,
            DstAccessMask = AccessFlags.MemoryReadBit,
            SrcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
            DstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
        };
        vk.CmdPipelineBarrier(cmdBuf,
            PipelineStageFlags.TransferBit,
            PipelineStageFlags.AllCommandsBit,
            0,
            0,
            null,
            1,
            &BufferMemoryBarriers3,
            0u,
            null);
        var r=currentbyteOffset;
        currentbyteOffset += (uint)dataLength;
        EndSingleTimeCommands(cmdBuf);
        return r;
    }
    

}
}