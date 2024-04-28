
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Vortice.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using static Vortice.Vulkan.Vulkan;
namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public const uint ComputeOutSSBOStartOffset = 64;
    public const uint ComputeInSSBOStartOffset = 64;
    public const bool BufferDEBUG = false;
    
    
    private static unsafe void CreateComputeResources()
    {
        if (!DrawIndirectCountAvaliable)
        {
            // create readback
            var readbackSize = 64;
            CreateBuffer((ulong) readbackSize, VkBufferUsageFlags.TransferDst,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                out GlobalData.ReadBackBuffer, out GlobalData.ReadBackMemory);
            fixed(void** ptr = &GlobalData.ReadBackBufferPtr)
                vkMapMemory(device, GlobalData.ReadBackMemory, 0, (ulong) readbackSize,0, ptr)
                    .Expect("failed to map memory!");
            CleanupStack.Push(()=>CleanupBufferImmediately(GlobalData.ReadBackBuffer, GlobalData.ReadBackMemory));
        }
        
        var computeShaderCode = File.ReadAllBytes(AssetsPath + "/shaders/compiled/PreRender.comp.spv");
        var computeMOdule = CreateShaderModule(computeShaderCode);
        var computeShaderStageInfo = new VkPipelineShaderStageCreateInfo()
        {
            stage = VkShaderStageFlags.Compute,
            module = computeMOdule,
            pName = (sbyte*) SilkMarshal.StringToPtr("main")
        };

        var descriptorSetLayoutBinding = stackalloc VkDescriptorSetLayoutBinding[]
        {
            new VkDescriptorSetLayoutBinding
            {
                //in data
                binding = BindingPoints.GPU_Compute_Input_Data,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Compute
            },
            new VkDescriptorSetLayoutBinding
            {
                //out renderindirect
                binding = BindingPoints.GPU_Compute_Output_Data,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Compute | VkShaderStageFlags.Vertex
            },
            new VkDescriptorSetLayoutBinding
            {
                // meshDB
                binding = BindingPoints.GPU_Compute_Input_Mesh,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.Compute
            },
            // new DescriptorSetLayoutBinding()
            // {
            //     binding = BindingPoints.GPU_Compute_Output_Secondary,
            //     DescriptorType = DescriptorType.StorageBuffer,
            //     DescriptorCount = 1,
            //     StageFlags = ShaderStageFlags.Compute,
            // }
        };
        var pBindingFlags= stackalloc VkDescriptorBindingFlags[]
        {
            VkDescriptorBindingFlags.UpdateUnusedWhilePending,
            VkDescriptorBindingFlags.UpdateUnusedWhilePending,
            VkDescriptorBindingFlags.UpdateUnusedWhilePending,
            // DescriptorBindingFlags.None
        };
        var descriptorSetLayoutBindingFlagsCreateInfo = new VkDescriptorSetLayoutBindingFlagsCreateInfo()
        {
            bindingCount = 3,
            pBindingFlags = pBindingFlags
        };
        var descriptorSetLayoutCreateInfo = new VkDescriptorSetLayoutCreateInfo()
        {
            bindingCount = 3,
            pBindings = descriptorSetLayoutBinding,
            pNext =&descriptorSetLayoutBindingFlagsCreateInfo 
        };
        vkCreateDescriptorSetLayout(device, &descriptorSetLayoutCreateInfo, null, out var computeDescriptorSetLayout);
        ComputeDescriptorSetLayout = computeDescriptorSetLayout;
        CleanupStack.Push(()=>vkDestroyDescriptorSetLayout(device,ComputeDescriptorSetLayout, null));
        

        var layouts = stackalloc VkDescriptorSetLayout[] {computeDescriptorSetLayout};
        var allocInfo = new VkDescriptorSetAllocateInfo
        {
            descriptorPool = DescriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = layouts,
        };
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            VkDescriptorSet computeDescriptorSet = default;
            vkAllocateDescriptorSets(device, &allocInfo,  &computeDescriptorSet)
                .Expect("failed to allocate descriptor sets!");
            FrameData[i].descriptorSets.Compute = computeDescriptorSet;
        }
        //UpdateComputeSSBODescriptors(0, 0, 0, 0);



        // vkCreateComputePipelines(device, default, 1, &computePipelineInfo, null, out var pipeline);
        // ComputePipeline = pipeline;
        (ComputePipeline,ComputePipelineLayout)=CreateComputePSO(computeShaderStageInfo,new(layouts,1));
        CleanupStack.Push(()=>vkDestroyPipelineLayout(device,ComputePipelineLayout, null));
        CleanupStack.Push(()=>vkDestroyPipeline(device,ComputePipeline, null));
        
        EnsureMeshRelatedBuffersAreSized();
        EnsureRenderObjectRelatedBuffersAreSized(5);
        vkDestroyShaderModule(device, computeMOdule, null);
        CleanupStack.Push(()=>
        {
            for (int i = 0; i < FRAME_OVERLAP; i++) CleanupHostRenderObjectMemory(i);
        });
        CleanupStack.Push(()=>CleanupBufferImmediately(GlobalData.MeshInfoBuffer, GlobalData.MeshInfoBufferMemory));
        CleanupStack.Push(() => CleanupDeviceRenderObjectMemory(GlobalData.deviceRenderObjectsBuffer,
            GlobalData.deviceRenderObjectsMemory,
            GlobalData.deviceIndirectDrawBuffer,
            GlobalData.deviceIndirectDrawBufferMemory));
        
        SilkMarshal.FreeString((IntPtr) computeShaderStageInfo.pName);
    }
    
    
    
    

    private static unsafe void UpdateComputeSSBODescriptors(ulong inRange, ulong outRange)
    {
        var inputBuffer = new VkDescriptorBufferInfo
        {
            buffer = GlobalData.deviceRenderObjectsBuffer,
            offset = 0,
            range = inRange, //todo update live
        };
        var OutputBuffer = new VkDescriptorBufferInfo
        {
            buffer = GlobalData.deviceIndirectDrawBuffer,
            offset = 0,
            range = outRange, //todo update live
        };

        var MeshInfoBuffer = new VkDescriptorBufferInfo
        {
            buffer = GlobalData.MeshInfoBuffer,
            offset = 0,
            range = (ulong) (GlobalData.MeshInfoBufferSize * sizeof(GPUStructs.MeshInfo)) ,
        };
        var OutputBufferForGfx = new VkDescriptorBufferInfo
        {
            buffer = GlobalData.deviceIndirectDrawBuffer,
            offset = 0,
            range = outRange,
        };


        var descriptorWrites = stackalloc VkWriteDescriptorSet[]
        {
            new()
            {
                dstSet = GetCurrentFrame().descriptorSets.Compute,
                dstBinding = BindingPoints.GPU_Compute_Input_Data,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                pBufferInfo = &inputBuffer,
            },
            new()
            {
                dstSet = GetCurrentFrame().descriptorSets.Compute,
                dstBinding = BindingPoints.GPU_Compute_Output_Data,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                pBufferInfo = &OutputBuffer,
            },
            new()
            {
                dstSet = GetCurrentFrame().descriptorSets.Compute,
                dstBinding = BindingPoints.GPU_Compute_Input_Mesh,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                pBufferInfo = &MeshInfoBuffer,
            },
            new() //gfx 
            {
                dstSet = GetCurrentFrame().descriptorSets.GFX,
                dstBinding = BindingPoints.GPU_Gfx_Input_Indirect,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.StorageBuffer,
                descriptorCount = 1,
                pBufferInfo = &OutputBufferForGfx,
            },
        };
        vkUpdateDescriptorSets(device, 4, descriptorWrites, 0, null);
        // vkUpdateDescriptorSets(device, 1, descriptorWrites[0], 0, null);
        // vkUpdateDescriptorSets(device,1, descriptorWrites[1], 0, null);
        // vkUpdateDescriptorSets(device,1, descriptorWrites[2], 0, null);
        // vkUpdateDescriptorSets(device,1, descriptorWrites[3], 0, null);
        
        
    }

    public static unsafe void EnsureRenderObjectRelatedBuffersAreSized(int minimumSize)
    {
        var ROtarget = Math.Max(10,minimumSize);
        var hostCurrentBufferSize = GetCurrentFrame().hostRenderObjectsBufferSize;
        if (hostCurrentBufferSize <= ROtarget)
        {
            CleanupHostRenderObjectMemory(CurrentFrameIndex);

            var newbufsize = Math.Max(ROtarget, hostCurrentBufferSize * 2);
            var newBufSizeInBytes = newbufsize * sizeof(GPUStructs.ComputeInput) + (int) ComputeInSSBOStartOffset;
            fixed (FrameData* frameData = &FrameData[CurrentFrameIndex])
            {
                CreateBuffer((ulong) newBufSizeInBytes,
                    VkBufferUsageFlags.TransferSrc |
                    VkBufferUsageFlags.TransferDst |
                    VkBufferUsageFlags.StorageBuffer,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                    &frameData->hostRenderObjectsBuffer, &frameData->hostRenderObjectsMemory);
                vkMapMemory(device, frameData->hostRenderObjectsMemory, 0, (ulong) newBufSizeInBytes, 0,
                        &frameData->hostRenderObjectsBufferPtr)
                    .Expect("failed to map memory!");
                frameData->hostRenderObjectsBufferSize = newbufsize;
                frameData->hostRenderObjectsBufferSizeInBytes=newBufSizeInBytes;
            }
        }

        if (GlobalData.deviceRenderObjectsBufferSize <= ROtarget)
        {
            var newbufsize = Math.Max(ROtarget, hostCurrentBufferSize * 2);
            var newBufSizeInBytes_RO = newbufsize * sizeof(GPUStructs.ComputeInput) + (int) ComputeInSSBOStartOffset;
            var newBufSizeInBytes_CmdDII=newbufsize * sizeof(GPUStructs.ComputeDrawOutput) + (int) ComputeOutSSBOStartOffset;
            
            var currentDeviceRenderObjectsBuffer = GlobalData.deviceRenderObjectsBuffer;
            var currentDeviceRenderObjectsBufferMemory = GlobalData.deviceRenderObjectsMemory;

            var currentDeviceIndirectDrawBuffer = GlobalData.deviceIndirectDrawBuffer;
            var currentDeviceIndirectDrawBufferMemory = GlobalData.deviceIndirectDrawBufferMemory;

            FrameCleanup[CurrentFrameIndex] // to be deleted once the last frame to utilize them is completed
                += () =>
                {
                    CleanupDeviceRenderObjectMemory(currentDeviceRenderObjectsBuffer,
                        currentDeviceRenderObjectsBufferMemory, currentDeviceIndirectDrawBuffer,
                        currentDeviceIndirectDrawBufferMemory);
                };

            CreateBuffer( //device renderobject buffer
                (ulong) newBufSizeInBytes_RO,
                VkBufferUsageFlags.TransferSrc |
                VkBufferUsageFlags.TransferDst |
                VkBufferUsageFlags.StorageBuffer,
                VkMemoryPropertyFlags.DeviceLocal,
                // VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                out VkBuffer newDeviceRenderObjectsBuffer,
                out var newDeviceRenderObjectsBufferMemory);

            CreateBuffer( //device indirect draw buffer
                (ulong) newBufSizeInBytes_CmdDII,
                VkBufferUsageFlags.TransferSrc |
                VkBufferUsageFlags.TransferDst |
                VkBufferUsageFlags.StorageBuffer |
                VkBufferUsageFlags.IndirectBuffer,
                VkMemoryPropertyFlags.DeviceLocal, 
                // VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                out VkBuffer newDeviceIndirectDrawBuffer,
                out var newDeviceIndirectDrawBufferMemory);

            GlobalData.deviceRenderObjectsBuffer = newDeviceRenderObjectsBuffer;
            GlobalData.deviceRenderObjectsMemory = newDeviceRenderObjectsBufferMemory;
            GlobalData.deviceRenderObjectsBufferSize = newbufsize;
            GlobalData.deviceRenderObjectsBufferSizeInBytes = newBufSizeInBytes_RO;
            GlobalData.deviceIndirectDrawBuffer = newDeviceIndirectDrawBuffer;
            GlobalData.deviceIndirectDrawBufferMemory = newDeviceIndirectDrawBufferMemory;
            GlobalData.deviceIndirectDrawBufferSize = newbufsize;
            GlobalData.deviceIndirectDrawBufferSizeInBytes = newBufSizeInBytes_CmdDII;
            
            if (BufferDEBUG)
            {
                void* tmp;
                vkMapMemory(device, newDeviceRenderObjectsBufferMemory, 0, (ulong) newBufSizeInBytes_RO, 0, &tmp)
                    .Expect("failed to map memory!");
                Unsafe.InitBlock(tmp,0,(uint) newBufSizeInBytes_RO);
                GlobalData.DEBUG_deviceRenderObjectsBufferPtr = tmp;
                
                vkMapMemory(device, newDeviceIndirectDrawBufferMemory, 0, (ulong) newBufSizeInBytes_CmdDII, 0, &tmp)
                    .Expect("failed to map memory!");
                Unsafe.InitBlock(tmp,0,(uint) newBufSizeInBytes_RO);
                GlobalData.DEBUG_deviceIndirectDrawBufferPtr = tmp;
            }

            //write shader descriptor
            UpdateComputeSSBODescriptors((ulong) newBufSizeInBytes_RO, (ulong) newBufSizeInBytes_CmdDII);
            //and write all other frames' sets too
            RegisterActionOnAllOtherFrames(()=>UpdateComputeSSBODescriptors((ulong) newBufSizeInBytes_RO, (ulong) newBufSizeInBytes_CmdDII));
        }
    }

    private static unsafe void CleanupDeviceRenderObjectMemory(VkBuffer currentDeviceRenderObjectsBuffer,
        VkDeviceMemory currentDeviceRenderObjectsBufferMemory, VkBuffer currentDeviceIndirectDrawBuffer,
        VkDeviceMemory currentDeviceIndirectDrawBufferMemory)
    {
        vkDestroyBuffer(device, currentDeviceRenderObjectsBuffer, default);
        vkFreeMemory(device, currentDeviceRenderObjectsBufferMemory, default);

        vkDestroyBuffer(device, currentDeviceIndirectDrawBuffer, default);
        vkFreeMemory(device, currentDeviceIndirectDrawBufferMemory, default);
    }

    private static unsafe void CleanupHostRenderObjectMemory(int i)
   {
            if (FrameData[i].hostRenderObjectsMemory.Handle != 0)
                vkUnmapMemory(device, FrameData[i].hostRenderObjectsMemory);
            if (FrameData[i].hostRenderObjectsBuffer.Handle != 0)
                vkDestroyBuffer(device, FrameData[i].hostRenderObjectsBuffer, default);
            if (FrameData[i].hostRenderObjectsMemory.Handle != 0)
                vkFreeMemory(device, FrameData[i].hostRenderObjectsMemory, default);
        
    }

    public static unsafe void EnsureMeshRelatedBuffersAreSized()
    {
        if (GlobalData.MeshInfoBufferSize > GPURenderRegistry.Meshes.Count)//should be idempotent
        {
            return;
        }

        //check mat


        var oldMeshInfoBuffer = GlobalData.MeshInfoBuffer;
        var oldMeshInfoBufferMemory = GlobalData.MeshInfoBufferMemory;
        var nextSize = Math.Max(GPURenderRegistry.Meshes.Count+1, GlobalData.MeshInfoBufferSize * 2); //exponential growth
        var newSizeByte = (ulong) nextSize * (ulong) sizeof(MeshInfo);
        
        CreateBuffer(newSizeByte, VkBufferUsageFlags.StorageBuffer,
            VkMemoryPropertyFlags.HostVisible|VkMemoryPropertyFlags.HostCoherent,
            out GlobalData.MeshInfoBuffer, out GlobalData.MeshInfoBufferMemory);

        
        uint oldSize = (uint) GlobalData.MeshInfoBufferSize * (uint) sizeof(MeshInfo);
        GlobalData.MeshInfoBufferSize = nextSize;
        RegisterBufferForCleanup(oldMeshInfoBuffer, oldMeshInfoBufferMemory); 
        void* oldPtr = GlobalData.MeshInfoBufferPtr;
        void* t;
        vkMapMemory(device, GlobalData.MeshInfoBufferMemory, 0, newSizeByte, 0, &t)
            .Expect("failed to map memory!");
        GlobalData.MeshInfoBufferPtr = t;
        
        Unsafe.InitBlock(GlobalData.MeshInfoBufferPtr, 0, (uint) newSizeByte);
        if (oldMeshInfoBufferMemory.Handle != 0)
        {
            Unsafe.CopyBlock(GlobalData.MeshInfoBufferPtr, oldPtr, oldSize);
            vkUnmapMemory(device, oldMeshInfoBufferMemory);
        }
        else
        {
            Unsafe.InitBlock(GlobalData.MeshInfoBufferPtr, 0, (uint) newSizeByte);
        }
    }
    

    private static unsafe void RegisterBufferForCleanup(VkBuffer buffer, VkDeviceMemory memory)
    {
        FrameCleanup[(CurrentFrameIndex + FRAME_OVERLAP - 1) % FRAME_OVERLAP] 
            += () => CleanupBufferImmediately(buffer, memory);
    }
    private static unsafe void CleanupBufferImmediately(VkBuffer buffer, VkDeviceMemory memory)
    {
        vkDestroyBuffer(device, buffer, default);
        vkFreeMemory(device, memory, default);
    }
}