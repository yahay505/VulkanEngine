// #define GPUDEBG
using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;
public static partial class VKRender{
static class GPUDynamicBuffer
{
    public static VkBuffer stagingBuffer;
    public static VkDeviceMemory stagingMemory;
    public static unsafe void* stagingMemoryPtr;
    public static int sizeOfStagingMemory=0;
    
}
public class GPUDynamicBuffer<T> where T:unmanaged
{
    private readonly VkBufferUsageFlags _usage;
    private readonly VkMemoryPropertyFlags _properties;
    public VkBuffer buffer;
    public VkDeviceMemory memory;
    private int currentSize=0;
    private ulong currentSizeInBytes=>(ulong) (currentSize*Unsafe.SizeOf<T>());
    private uint currentbyteOffset=0;

    public unsafe void* DebugPtr;
    public GPUDynamicBuffer(ulong initialSizeInItems, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties)
    {
        _usage = usage|VkBufferUsageFlags.TransferDst|VkBufferUsageFlags.TransferSrc;
        _properties = properties;
#if GPUDEBG
        _properties &= ~VkMemoryPropertyFlags.DeviceLocal;
        _properties |= VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent;
#endif
        
        
        currentSize = (int) initialSizeInItems;
    
        CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
#if GPUDEBG

        unsafe
        {
            fixed (void** debugPtr = &DebugPtr)
                vkMapMemory(device, memory, 0, currentSizeInBytes, 0,
                    debugPtr);
        }
#endif

    }

    public unsafe uint Upload(Span<T> data, VkPipelineStageFlags dstStageMask)
    {
        var cmdBuf=BeginSingleTimeCommands();

        var dataLength = (uint)data.Length * Unsafe.SizeOf<T>();
        if (currentbyteOffset+dataLength > (int) currentSizeInBytes)
        {
            
            var oldbuf=buffer;
            var oldmem=memory;
            var oldbufsizeinbytes=currentSizeInBytes;
            var newSize = Math.Max(currentSize + data.Length,(currentSize * 2));
            currentSize = newSize; //auto sets currentSizeInBytes
            CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
#if GPUDEBG
            fixed (void** debugPtr = &DebugPtr)
                vkMapMemory(device, memory, 0, currentSizeInBytes, 0,
                    debugPtr);
#endif
            if (currentbyteOffset!=0)// if not empty
            {
                var vkBufferCopy = new VkBufferCopy()
                {
                    size = currentbyteOffset
                };
                vkCmdCopyBuffer(cmdBuf, oldbuf, buffer, 1, &vkBufferCopy);
                var BufferMemoryBarriers = new VkBufferMemoryBarrier()
                {
                    buffer = buffer,
                    size = currentSizeInBytes,
                    srcAccessMask = VkAccessFlags.TransferWrite,
                    dstAccessMask = VkAccessFlags.TransferWrite,
                    srcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
                    dstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
                };
                vkCmdPipelineBarrier(cmdBuf,
                    VkPipelineStageFlags.Transfer,
                    VkPipelineStageFlags.Transfer,
                    0,
                    0,
                    null,
                    1,
                    &BufferMemoryBarriers,
                    0u,
                    null);
            }

            RegisterBufferForCleanup(oldbuf, oldmem);
        }

        if (dataLength > GPUDynamicBuffer.sizeOfStagingMemory)
        {
            CleanupBufferImmediately(GPUDynamicBuffer.stagingBuffer, GPUDynamicBuffer.stagingMemory);
            CreateBuffer((ulong) dataLength,
                VkBufferUsageFlags.TransferSrc,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                out GPUDynamicBuffer.stagingBuffer,
                out GPUDynamicBuffer.stagingMemory);
            fixed (void** pstagingMemoryPtr = &GPUDynamicBuffer.stagingMemoryPtr)
                vkMapMemory(device, GPUDynamicBuffer.stagingMemory, 0, (ulong) dataLength, 0,
                pstagingMemoryPtr);
        }

        {
            data.CopyTo(new Span<T>(GPUDynamicBuffer.stagingMemoryPtr, data.Length));
            //memory barrier
            var BufferMemoryBarriers2 = new VkBufferMemoryBarrier()
            {
                buffer = GPUDynamicBuffer.stagingBuffer,
                size = (ulong) dataLength,
                srcAccessMask = VkAccessFlags.HostWrite,
                dstAccessMask = VkAccessFlags.TransferRead,
                srcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
                dstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
            };
            vkCmdPipelineBarrier(cmdBuf,
                VkPipelineStageFlags.Host,
                VkPipelineStageFlags.Transfer,
                0,
                0,
                null,
                1,
                &BufferMemoryBarriers2,
                0u,
                null);
            var vkBufferCopy = new VkBufferCopy()
            {
                size = (ulong) dataLength,
                srcOffset = 0,
                dstOffset = (ulong) currentbyteOffset
            };
            vkCmdCopyBuffer(cmdBuf, GPUDynamicBuffer.stagingBuffer, buffer, 1, &vkBufferCopy);
            //memory barrier
            var BufferMemoryBarriers3 = new VkBufferMemoryBarrier()
           {
                buffer = buffer,
                size = (ulong) dataLength,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.MemoryRead,
                srcQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily,
                dstQueueFamilyIndex = (uint) VKRender._familyIndices.graphicsFamily
            };
            vkCmdPipelineBarrier(cmdBuf,
                VkPipelineStageFlags.Transfer,
                dstStageMask,
                0,
                0,
                null,
                1,
                &BufferMemoryBarriers3,
                0u,
                null);
        }
        var offset=currentbyteOffset/(uint)Unsafe.SizeOf<T>();
        currentbyteOffset += (uint)dataLength;
        EndSingleTimeCommands(cmdBuf);
        return offset;
    }
    

}
}