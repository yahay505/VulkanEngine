
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public const uint ComputeOutSSBOStartOffset = 64;
    public const uint ComputeInSSBOStartOffset = 64;

    private static unsafe void CreateComputeResources()
    {
        if (!DrawIndirectCountAvaliable)
        {
            // create readback
            var readbackSize = 64;
            CreateBuffer((ulong) readbackSize, BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                out GlobalData.ReadBackBuffer, out GlobalData.ReadBackMemory);
            fixed(void** ptr = &GlobalData.ReadBackBufferPtr)
                vk.MapMemory(device, GlobalData.ReadBackMemory, 0, (ulong) readbackSize, 0, ptr)
                    .Expect("failed to map memory!");
            CleanupStack.Push(()=>CleanupBuffer(GlobalData.ReadBackBuffer, GlobalData.ReadBackMemory));
        }
        var computeShaderCode = File.ReadAllBytes(AssetsPath + "/shaders/compiled/PreRender.comp.spv");
        var computeMOdule = CreateShaderModule(computeShaderCode);
        var computeShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = computeMOdule,
            PName = (byte*) SilkMarshal.StringToPtr("main")
        };

        var descriptorSetLayoutBinding = stackalloc DescriptorSetLayoutBinding[]
        {
            new DescriptorSetLayoutBinding
            {
                //in data
                Binding = BindingPoints.GPU_Compute_Input_Data,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit
            },
            new DescriptorSetLayoutBinding
            {
                //out renderindirect
                Binding = BindingPoints.GPU_Compute_Output_Data,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit | ShaderStageFlags.VertexBit
            },
            new DescriptorSetLayoutBinding
            {
                // meshDB
                Binding = BindingPoints.GPU_Compute_Input_Mesh,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit
            },
            new DescriptorSetLayoutBinding()
            {
                Binding = BindingPoints.GPU_Compute_Output_Secondary,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit,
            }
        };
        var pBindingFlags= stackalloc DescriptorBindingFlags[]
        {
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            DescriptorBindingFlags.None
        };
        var descriptorSetLayoutBindingFlagsCreateInfo = new DescriptorSetLayoutBindingFlagsCreateInfo()
        {
            SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            BindingCount = 4,
            PBindingFlags = pBindingFlags
        };
        var descriptorSetLayoutCreateInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 3,
            PBindings = descriptorSetLayoutBinding,
            PNext =&descriptorSetLayoutBindingFlagsCreateInfo 
        };
        vk.CreateDescriptorSetLayout(device, &descriptorSetLayoutCreateInfo, null, out var computeDescriptorSetLayout);
        ComputeDescriptorSetLayout = computeDescriptorSetLayout;
        CleanupStack.Push(()=>vk.DestroyDescriptorSetLayout(device,ComputeDescriptorSetLayout, null));

        var computePipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &computeDescriptorSetLayout
        };

        vk.CreatePipelineLayout(device, &computePipelineLayoutInfo, null, out var layout);
        ComputePipelineLayout = layout;
        CleanupStack.Push(()=>vk.DestroyPipelineLayout(device,ComputePipelineLayout, null));

        var layouts = stackalloc DescriptorSetLayout[] {computeDescriptorSetLayout};
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = DescriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts,
        };
        vk.AllocateDescriptorSets(device, &allocInfo, out var computeDescriptorSet)
            .Expect("failed to allocate descriptor sets!");

        ComputeDescriptorSet = computeDescriptorSet;
        //UpdateComputeSSBODescriptors(0, 0, 0, 0);


        var computePipelineInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Stage = computeShaderStageInfo,
            Layout = layout,
        };

        vk.CreateComputePipelines(device, default, 1, &computePipelineInfo, null, out var pipeline);
        ComputePipeline = pipeline;
        CleanupStack.Push(()=>vk.DestroyPipeline(device,ComputePipeline, null));
        
        EnsureMeshRelatedBuffersAreSized();
        ResizeRenderObjectRelatedBuffer(10, 0);
        vk.DestroyShaderModule(device, computeMOdule, null);
        CleanupStack.Push(()=>
        {
            for (int i = 0; i < FRAME_OVERLAP; i++) CleanupHostRenderObjectMemory(i);
        });
        CleanupStack.Push(()=>CleanupBuffer(GlobalData.MeshInfoBuffer, GlobalData.MeshInfoBufferMemory));
        CleanupStack.Push(() => CleanupDeviceRenderObjectMemory(GlobalData.deviceRenderObjectsBuffer,
            GlobalData.deviceRenderObjectsMemory,
            GlobalData.deviceIndirectDrawBuffer,
            GlobalData.deviceIndirectDrawBufferMemory));
        
        SilkMarshal.FreeString((IntPtr) computeShaderStageInfo.PName);
    }

    private static unsafe void UpdateComputeSSBODescriptors(ulong inOffset, ulong inRange, ulong outOffset,
        ulong outRange)
    {
        Console.WriteLine("updating compute descriptors Stopping GPU");
        vk.DeviceWaitIdle(device);
        EnsureMeshRelatedBuffersAreSized();
        var inputBuffer = new DescriptorBufferInfo
        {
            Buffer = GlobalData.deviceRenderObjectsBuffer,
            Offset = inOffset,
            Range = inRange, //todo update live
        };
        var OutputBuffer = new DescriptorBufferInfo
        {
            Buffer = GlobalData.deviceIndirectDrawBuffer,
            Offset = outOffset,
            Range = outRange, //todo update live
        };
        var MeshInfoBuffer = new DescriptorBufferInfo
        {
            Buffer = GlobalData.MeshInfoBuffer,
            Offset = 0,
            Range = Vk.WholeSize, //todo seperate update to another function and allow for dynamic mesh count
        };
        var OutputBufferForGfx = new DescriptorBufferInfo
        {
            Buffer = GlobalData.deviceIndirectDrawBuffer,
            Offset = ComputeOutSSBOStartOffset,
            Range = outRange - ComputeOutSSBOStartOffset,
        };


        var descriptorWrites = stackalloc WriteDescriptorSet[]
        {
            new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = ComputeDescriptorSet,
                DstBinding = BindingPoints.GPU_Compute_Input_Data,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &inputBuffer,
            },
            new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = ComputeDescriptorSet,
                DstBinding = BindingPoints.GPU_Compute_Output_Data,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &OutputBuffer,
            },
            new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = ComputeDescriptorSet,
                DstBinding = BindingPoints.GPU_Compute_Input_Mesh,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &MeshInfoBuffer,
            },
            new() //gfx 
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = GfxDescriptorSet,
                DstBinding = BindingPoints.GPU_Gfx_Input_Indirect,
                DstArrayElement = 0,
                DescriptorType = DescriptorType.StorageBuffer,
                DescriptorCount = 1,
                PBufferInfo = &OutputBufferForGfx,
            },
        };
        vk.UpdateDescriptorSets(device, 4, descriptorWrites, 0, null);
        // vk.UpdateDescriptorSets(device, 1, descriptorWrites[0], 0, null);
        // vk.UpdateDescriptorSets(device,1, descriptorWrites[1], 0, null);
        // vk.UpdateDescriptorSets(device,1, descriptorWrites[2], 0, null);
        // vk.UpdateDescriptorSets(device,1, descriptorWrites[3], 0, null);
        
        
    }

    private static unsafe void ResizeRenderObjectRelatedBuffer(int neededBufferSizeInItems,
        int currentBufferSizeInItems)
    {
        CleanupHostRenderObjectMemory(CurrentFrameIndex);

        var newbufsize = Math.Max(neededBufferSizeInItems, currentBufferSizeInItems * 2);
        var newBufSizeInBytes = newbufsize * sizeof(GPUStructs.ComputeInput)+(int) ComputeInSSBOStartOffset;
        fixed (FrameData* frameData = &FrameData[CurrentFrameIndex])
        {
            CreateBuffer((ulong) newBufSizeInBytes,
                BufferUsageFlags.TransferSrcBit |
                BufferUsageFlags.TransferDstBit |
                BufferUsageFlags.StorageBufferBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                &frameData->hostRenderObjectsBuffer, &frameData->hostRenderObjectsMemory);
            vk.MapMemory(device, frameData->hostRenderObjectsMemory, 0, (ulong) newBufSizeInBytes, 0,
                    &frameData->hostRenderObjectsBufferPtr)
                .Expect("failed to map memory!");
            frameData->hostRenderObjectsBufferSize = newbufsize;
        }

        var currentDeviceRenderObjectsBuffer = GlobalData.deviceRenderObjectsBuffer;
        var currentDeviceRenderObjectsBufferMemory = GlobalData.deviceRenderObjectsMemory;

        var currentDeviceIndirectDrawBuffer = GlobalData.deviceIndirectDrawBuffer;
        var currentDeviceIndirectDrawBufferMemory = GlobalData.deviceIndirectDrawBufferMemory;

        FrameCleanup
                [(CurrentFrameIndex + FRAME_OVERLAP - 1) % FRAME_OVERLAP] // to be deleted once the last frame to utilize them is completed
            += () =>
            {
                CleanupDeviceRenderObjectMemory(currentDeviceRenderObjectsBuffer, currentDeviceRenderObjectsBufferMemory, currentDeviceIndirectDrawBuffer, currentDeviceIndirectDrawBufferMemory);
            };

        CreateBuffer( //device renderobject buffer
            (ulong) newBufSizeInBytes,
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.TransferDstBit |
            BufferUsageFlags.StorageBufferBit,
            MemoryPropertyFlags.DeviceLocalBit, out var newDeviceRenderObjectsBuffer,
            out var newDeviceRenderObjectsBufferMemory);

        var newOutBuffSizeInBytes = ((ulong) newbufsize) * ((ulong) sizeof(GPUStructs.ComputeOutput)+ComputeOutSSBOStartOffset);
        CreateBuffer( //device indirect draw buffer
            newOutBuffSizeInBytes,
            BufferUsageFlags.TransferSrcBit |
            BufferUsageFlags.TransferDstBit |
            BufferUsageFlags.StorageBufferBit |
            BufferUsageFlags.IndirectBufferBit,
            MemoryPropertyFlags.DeviceLocalBit, out var newDeviceIndirectDrawBuffer,
            out var newDeviceIndirectDrawBufferMemory);

        GlobalData.deviceRenderObjectsBuffer = newDeviceRenderObjectsBuffer;
        GlobalData.deviceRenderObjectsMemory = newDeviceRenderObjectsBufferMemory;
        GlobalData.deviceIndirectDrawBuffer = newDeviceIndirectDrawBuffer;
        GlobalData.deviceIndirectDrawBufferMemory = newDeviceIndirectDrawBufferMemory;

        //write shader descriptor
        UpdateComputeSSBODescriptors(0, (ulong) newBufSizeInBytes, 0, newOutBuffSizeInBytes);
    }

    private static unsafe void CleanupDeviceRenderObjectMemory(Buffer currentDeviceRenderObjectsBuffer,
        DeviceMemory currentDeviceRenderObjectsBufferMemory, Buffer currentDeviceIndirectDrawBuffer,
        DeviceMemory currentDeviceIndirectDrawBufferMemory)
    {
        vk.DestroyBuffer(device, currentDeviceRenderObjectsBuffer, default);
        vk.FreeMemory(device, currentDeviceRenderObjectsBufferMemory, default);

        vk.DestroyBuffer(device, currentDeviceIndirectDrawBuffer, default);
        vk.FreeMemory(device, currentDeviceIndirectDrawBufferMemory, default);
    }

    private static unsafe void CleanupHostRenderObjectMemory(int i)
    {

            if (FrameData[i].hostRenderObjectsMemory.Handle != 0)
                vk.UnmapMemory(device, FrameData[i].hostRenderObjectsMemory);
            if (FrameData[i].hostRenderObjectsBuffer.Handle != 0)
                vk.DestroyBuffer(device, FrameData[i].hostRenderObjectsBuffer, default);
            if (FrameData[i].hostRenderObjectsMemory.Handle != 0)
                vk.FreeMemory(device, FrameData[i].hostRenderObjectsMemory, default);
        
    }

    public static unsafe void EnsureMeshRelatedBuffersAreSized()
    {
        if (GlobalData.MeshInfoBufferSize > RenderManager.Meshes.Count)//should be idempotent
        {
            return;
        }

        //check mat


        var oldMeshInfoBuffer = GlobalData.MeshInfoBuffer;
        var oldMeshInfoBufferMemory = GlobalData.MeshInfoBufferMemory;
        var nextSize = Math.Max(RenderManager.Meshes.Count+1, GlobalData.MeshInfoBufferSize * 2); //exponential growth
        var newSize = (ulong) nextSize * (ulong) sizeof(MeshInfo);
        CreateBuffer(newSize, BufferUsageFlags.StorageBufferBit,
            MemoryPropertyFlags.HostVisibleBit,
            out var newMeshInfoBuffer, out var newMeshInfoBufferMemory);
        GlobalData.MeshInfoBuffer = newMeshInfoBuffer;
        GlobalData.MeshInfoBufferMemory = newMeshInfoBufferMemory;
        uint oldSize = (uint) GlobalData.MeshInfoBufferSize * (uint) sizeof(MeshInfo);
        GlobalData.MeshInfoBufferSize = nextSize;
        FrameCleanup
                [CurrentFrameIndex + FRAME_OVERLAP - 1 % FRAME_OVERLAP] // to be deleted once the last frame to utilize them is completed
            += () => { CleanupBuffer(oldMeshInfoBuffer, oldMeshInfoBufferMemory); };
        void* newPtr = (void*) 0;
        vk.MapMemory(device, newMeshInfoBufferMemory, 0, newSize, 0, &newPtr)
            .Expect("failed to map memory!");
        if (oldMeshInfoBufferMemory.Handle != 0)
        {
            Unsafe.CopyBlock(newPtr, GlobalData.MeshInfoBufferPtr, oldSize);
                vk.UnmapMemory(device, GlobalData.MeshInfoBufferMemory);
        }

        GlobalData.MeshInfoBufferPtr = newPtr;
    }
    

    
    private static unsafe void CleanupBuffer(Buffer oldMeshInfoBuffer, DeviceMemory oldMeshInfoBufferMemory)
    {
        vk.DestroyBuffer(device, oldMeshInfoBuffer, default);
        vk.FreeMemory(device, oldMeshInfoBufferMemory, default);
    }
}