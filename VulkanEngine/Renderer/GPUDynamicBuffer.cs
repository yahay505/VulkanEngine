// #define GPUDEBG
using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static VulkanEngine.Renderer.GPUDEBUG;
namespace VulkanEngine.Renderer;
public static partial class VKRender{
static class GPUDynamicBuffer
{
    public static VkBuffer stagingBuffer;
    public static VkDeviceMemory stagingMemory;
    public static unsafe void* stagingMemoryPtr;
    public static int sizeOfStagingMemory=0;
    public static int stagingMemoryUsed;
}
public class GPUDynamicBuffer<T> where T:unmanaged
{
    private readonly VkBufferUsageFlags _usage;
    private readonly VkMemoryPropertyFlags _properties;
    public VkBuffer buffer;
    public VkDeviceMemory memory;
    private int currentSize=0;
    private unsafe sbyte* name;
    private int nameLenght;
    private int variantNo;
    private ulong currentSizeInBytes=>(ulong) (currentSize*Unsafe.SizeOf<T>());
    private uint currentbyteOffset=0;
    public unsafe void* DebugPtr;
    public bool isMapped = false;
    
    
    public GPUDynamicBuffer(ulong initialSizeInItems, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties,ReadOnlySpan<byte> name)
    {
        unsafe
        {
            _usage = usage|VkBufferUsageFlags.TransferDst|VkBufferUsageFlags.TransferSrc;
            _properties = properties;
#if GPUDEBG
        _properties &= ~VkMemoryPropertyFlags.DeviceLocal;
        _properties |= VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent;
#endif
            this.name = name.As<byte,sbyte>().GetPointer();
            this.nameLenght = name.Length;
        
            currentSize = (int) initialSizeInItems;
    
            CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
            if(name!=default)
            {
                MarkObject(buffer,name,variantNo);
                MarkObject(memory,name,variantNo);
            }
#if GPUDEBG

        unsafe
        {
            fixed (void** debugPtr = &DebugPtr)
                vkMapMemory(device, memory, 0, currentSizeInBytes, 0,
                    debugPtr);
        }
#endif
        }
    }
    public unsafe uint Upload(Span<T> data, VkPipelineStageFlags dstStageMask)
    {
        if (Interlocked.CompareExchange(ref GPUDynamicBuffer.stagingMemoryUsed,1,0)!=0)
        {
            throw new Exception();
        }
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
            variantNo++;
            MarkObject(buffer,name,variantNo);
            MarkObject(memory,name,variantNo);
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
            MarkObject(GPUDynamicBuffer.stagingBuffer,"GPUDynamicBuffer.stagingBuffer"u8);
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
        if (Interlocked.CompareExchange(ref GPUDynamicBuffer.stagingMemoryUsed,0,1)!=1)
        {
            throw new Exception();
        }
        return offset;
    }

    public unsafe T* MapOrGetAdress()
    {
        ((_properties & VkMemoryPropertyFlags.HostVisible) != 0).Assert();
        if (isMapped) return (T*) DebugPtr;
        fixed (void** a = &DebugPtr)
            vkMapMemory(device, memory, 0, currentSizeInBytes, 0, a);
        isMapped = true;
        return (T*) DebugPtr;
    }
}
}