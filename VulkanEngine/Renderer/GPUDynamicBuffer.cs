using System.Collections;
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

    public GPUDynamicBuffer(ulong initialSizeInItems, BufferUsageFlags usage, MemoryPropertyFlags properties)
    {
        _usage = usage|BufferUsageFlags.TransferDstBit|BufferUsageFlags.TransferSrcBit;
        _properties = properties;
        currentSize = (int) initialSizeInItems;
        
        CreateBuffer(currentSizeInBytes,_usage,_properties,out buffer,out memory);
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
            vk.CmdCopyBuffer(cmdBuf, oldbuf, buffer, 1, new BufferCopy()
            {
                Size = oldbufsizeinbytes
            });
            
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
        vk.CmdCopyBuffer(cmdBuf, GPUDynamicBuffer.stagingBuffer, buffer, 1, new BufferCopy()
        {
            Size = (ulong) dataLength,
            SrcOffset = 0,
            DstOffset = (ulong) currentbyteOffset
        });
        var r=currentbyteOffset;
        currentbyteOffset += (uint)dataLength;
        EndSingleTimeCommands(cmdBuf);
        return r;
    }
    

}
}