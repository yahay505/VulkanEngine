using Vortice.SPIRV.Reflect;
using Vortice.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using static Vortice.Vulkan.Vulkan;
using static VulkanEngine.Renderer.GPUDEBUG;

namespace VulkanEngine.Renderer;

public static class MaterialManager
{
    private const int materialSize = 32;
    private static HostCachedBuffer buffer;
    private static VkDescriptorBufferInfo bufferInfo;

    public static void Init()
    {
        buffer = new(new(new byte[320]),VkBufferUsageFlags.StorageBuffer,VkMemoryPropertyFlags.DeviceLocal,"Material Info Buffer"u8);
    }

    public static unsafe VkWriteDescriptorSet Bind(uint baseBindNo)
    {
        bufferInfo = new()
        {
            buffer = buffer.slave.buffer,
            offset = 0,
            range = 0,
        };
        VkWriteDescriptorSet write = new()
        {
            descriptorCount = 1,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.StorageBuffer,
            dstBinding = baseBindNo,
            dstSet = 0,
            pBufferInfo = bufferInfo.ptr(),
        };

        return write;
    }
    public static void SetMaterial(int MatID, ReadOnlySpan<byte> material)
    {
        buffer[(MatID * materialSize)..(MatID * materialSize + materialSize)] = material;
    }
    /// <summary>
    /// Does <b><i>NOT</i></b> issue barrier before writes (assumes transferWrite access)<br/>
    /// Does issue buffer Memory barrier after write
    /// </summary>
    /// <param name="commandBuffer"></param>
    /// <param name="dstStageMask"></param>
    /// <param name="dstAccessMask"></param>
    public static void Sync(VkCommandBuffer commandBuffer,VkPipelineStageFlags dstStageMask, VkAccessFlags dstAccessMask)
    {
        buffer.UploadUpdates(commandBuffer,dstStageMask, dstAccessMask);
    }
    
}

class HostCachedBuffer
{
    public Memory<byte> master;
    public GPUOnlyBuffer slave;
    public List<Range> modrange;
    

    public HostCachedBuffer(Memory<byte> backing, VkBufferUsageFlags usageFlags, VkMemoryPropertyFlags properties, ReadOnlySpan<byte> name)
    {
        this.master = backing;
        modrange = new();
        modrange.Add(0..master.Length);
        slave = new GPUOnlyBuffer((uint) backing.Length, usageFlags, properties, name);
    }

    public ReadOnlySpan<byte> this[Range a]
    {
        get => default;
        set
        {
            value.CopyTo(master.Span[a]);
            modrange.Add(a);
        }
    }

    /// <summary>
    /// Does <b><i>NOT</i></b> issue barrier before writes (assumes transferWrite access)<br/>
    /// Does issue buffer Memory barrier after write
    /// </summary>
    /// 
    /// <param name="commandBuffer"></param>
    /// <param name="dstStageMask"></param>
    /// <param name="dstAccessMask"></param>
    /// <exception cref="NotImplementedException"></exception>
    public unsafe void UploadUpdates(VkCommandBuffer commandBuffer, VkPipelineStageFlags dstStageMask, VkAccessFlags dstAccessMask)
    {
        for (int z = modrange.Count-1; z >= 1; z--) {
            for (int y = z - 1; y >= 0; y--) {
                if (modrange[z].DoesOverlap(modrange[y])) {
                    modrange[y] = (modrange[z].Consolidate(modrange[y]));
                    modrange.RemoveAt(z);
                    break;
                }
            }
        }
        BeginRegion(commandBuffer,"upload"u8);

        var postBarrier = stackalloc VkBufferMemoryBarrier[modrange.Count];
        for (var i = 0; i < modrange.Count; i++)
        {
            var range = modrange[i];
            var (start, end) = range.Decompose();
            var size = (ulong) (end - start);
            if (size>60000)
            {
                throw new NotImplementedException();
            }
            vkCmdUpdateBuffer(commandBuffer,slave.buffer,(ulong) start,size,&master.Span.GetPointerUnsafe()[start]);
            
            postBarrier[i] = new()
            {
                buffer = slave.buffer,
                srcAccessMask = VkAccessFlags.TransferWrite,
                dstAccessMask = dstAccessMask,
                offset = (ulong) start,
                size = size,
            };
        }
        vkCmdPipelineBarrier(commandBuffer,
            VkPipelineStageFlags.Transfer,
            dstStageMask,
            VkDependencyFlags.None,
            0,
            null,
            (uint) modrange.Count,
            postBarrier,
            0,
            null);
        modrange.Clear();
        EndRegion(commandBuffer);
    }
    
}

class GPUOnlyBuffer
{
    public VkDeviceMemory memory;
    public VkBuffer buffer;
    public uint size;
    private readonly VkBufferUsageFlags _usage;
    private readonly VkMemoryPropertyFlags _properties;
    private readonly unsafe sbyte* namePtr;
    private readonly int nameLenght;
    private unsafe ReadOnlySpan<byte> name => new (namePtr, nameLenght);

    public GPUOnlyBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties,ReadOnlySpan<byte> name)
    {
        unsafe
        {
            _usage = usage|VkBufferUsageFlags.TransferDst|VkBufferUsageFlags.TransferSrc;
            _properties = properties;
            this.size = size;
            VKRender.CreateBuffer(size,_usage,_properties,out buffer,out memory);
            this.namePtr = Interop.As<byte,sbyte>(name).GetPointer();
            this.nameLenght = name.Length;
            
            if(name!=default)
            {
                MarkObject(buffer,name);
                MarkObject(memory,name);
            }

        }
    }

    public unsafe bool TrySet(VkCommandBuffer commandBuffer, uint byteOffset, ReadOnlySpan<byte> data)
    {
        if (data.Length+byteOffset>size)
        {
            return false;
        }

        if (data.Length<60000)
        {
            vkCmdUpdateBuffer(commandBuffer,buffer,byteOffset,(ulong) data.Length,data.GetPointer());
        }
        else
        {
            throw new NotImplementedException();
        }

        return true;
    }
    
    
    public unsafe void Resize(VkCommandBuffer commandBuffer, uint target, ref Action cleanup)
    {
        var oldbuff = buffer;
        var oldMem = memory;
        var oldSize = size;
        size = target;
        var minSize = uint.Min(target, oldSize);
        VKRender.CreateBuffer(size,_usage,_properties,out buffer,out memory);
        if(namePtr!=default)
        {
            MarkObject(buffer,name);
            MarkObject(memory,name);
        }
        VkBufferMemoryBarrier oldBarr = new()
        {
            buffer = oldbuff,
            srcAccessMask = VkAccessFlags.TransferWrite,
            dstAccessMask = VkAccessFlags.TransferRead,
            offset = 0,
            size = minSize,
        };
        vkCmdPipelineBarrier(commandBuffer,
            VkPipelineStageFlags.Transfer,
            VkPipelineStageFlags.Transfer,
            VkDependencyFlags.None,
            0,null,
            1, &oldBarr,
            0,null);
        VkBufferCopy copy = new()
        {
            srcOffset = 0,
            dstOffset = 0,
            size = minSize,
        };
        vkCmdCopyBuffer(commandBuffer,oldbuff,buffer,1,&copy);
        VkBufferMemoryBarrier barr = new()
        {
            buffer = buffer,
            srcAccessMask = VkAccessFlags.TransferWrite,
            dstAccessMask = VkAccessFlags.TransferWrite,
            offset = 0,
            size = minSize,
        };
        vkCmdPipelineBarrier(commandBuffer,
            VkPipelineStageFlags.Transfer,
            VkPipelineStageFlags.Transfer,
            VkDependencyFlags.None,
            0,null,
            1, &barr,
            0,null);
        cleanup += () =>
        {
            vkDestroyBuffer(VKRender.device, oldbuff);
            vkFreeMemory(VKRender.device, oldMem);
        };
    }
    
}