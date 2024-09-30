// #define GPUDEBG

using System.Runtime.CompilerServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static VulkanEngine.Renderer2.infra.GPUDEBUG;
namespace VulkanEngine.Renderer2.infra;

public class GPUDynamicBuffer
{
    private readonly VkBufferUsageFlags _usage;
    private readonly VkMemoryPropertyFlags _properties;
    public VkBuffer buffer;
    public VkDeviceMemory memory;
    private ulong currentSize=0;
    private unsafe sbyte* name;
    private int nameLenght;
    private int variantNo;
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
        
            currentSize = initialSizeInItems;
    
            API.CreateBuffer(currentSize,_usage,_properties,out buffer,out memory);
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
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cb"></param>
    /// <param name="data"></param>
    /// <param name="dstStageMask"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="Exception"></exception>
    public unsafe uint Upload(VkCommandBuffer cb, Span<DefaultVertex> data, VkPipelineStageFlags dstStageMask)
    {


        var dataLength = (uint)data.Length;
        if (currentbyteOffset+dataLength > (int) currentSize)
        {
            
            var oldbuf=buffer;
            var oldmem=memory;
            var oldbufsizeinbytes=currentSize;
            var newSize = Math.Max(currentSize + (ulong) data.Length,(currentSize * 2));
            currentSize = newSize; //auto sets currentSizeInBytes
            API.CreateBuffer(currentSize,_usage,_properties,out buffer,out memory);
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
                vkCmdCopyBuffer(cb, oldbuf, buffer, 1, &vkBufferCopy);
                var BufferMemoryBarriers = new VkBufferMemoryBarrier()
                {
                    buffer = buffer,
                    size = currentSize,
                    srcAccessMask = VkAccessFlags.TransferWrite,
                    dstAccessMask = VkAccessFlags.TransferWrite,
                    srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
                    dstQueueFamilyIndex =VK_QUEUE_FAMILY_IGNORED
                };
                vkCmdPipelineBarrier(cb,
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

            Cleanup.RegisterForCleanupAfterCBCompletion(cb,oldbuf);
            Cleanup.RegisterForCleanupAfterCBCompletion(cb,oldmem);
        }

        if (dataLength > 60_000)
        {
            throw new NotImplementedException();
        }

        {
         
            vkCmdUpdateBuffer(cb, buffer, currentbyteOffset, (ulong) data.Length,data.ptr());
            //memory barrier
            var BufferMemoryBarriers3 = new VkBufferMemoryBarrier()
           {
                buffer = buffer,
                size = (ulong) dataLength,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = VkAccessFlags.MemoryRead,
                srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
                dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED
            };
            vkCmdPipelineBarrier(cb,
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
        var offset=currentbyteOffset;
        currentbyteOffset += (uint)dataLength;
        return offset;
    }

    public unsafe void* MapOrGetAdress()
    {
        ((_properties & VkMemoryPropertyFlags.HostVisible) != 0).Assert();
        if (isMapped) return DebugPtr;
        fixed (void** a = &DebugPtr)
            vkMapMemory(API.device, memory, 0, currentSize, 0, a);
        isMapped = true;
        return  DebugPtr;
    }
}
