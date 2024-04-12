using System.Diagnostics;
using System.Runtime.InteropServices;
using Pastel;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using SixLabors.ImageSharp.PixelFormats;
using VulkanEngine.Renderer.GPUStructs;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    public static void InitVulkan()
    {
        // CreateSurface();
        // // Analize
        // CreateSwapChain(true);
        // CreateSwapChainImageViews();
        CreateRenderPass(mainWindow);
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();

        AllocatePerFrameData();
        
        
        // CreateSwapchainFrameBuffers();
        CreateTextureImage();
        
        CreateTextureImageView();
        CreateTextureSampler();
        CreateUniformBuffers();
        CreateDescriptionPool();
        CreateDescriptorSets();

        CreateComputeResources();
        
    }


    public static unsafe void InitVulkanFirstPhase(byte** ppExt, int extC)
    {
        CreateInstance(ppExt,extC);
        SetupDebugMessenger();
    }
    public static void InitVulkanSecondPhase(SurfaceKHR surface)
    {
        DeviceInfo=DeviceRequirements.PickPhysicalDevice(surface);
        CreateLogicalDevice();
        AllocateGlobalData();

    }

    //todo remove
    static Image textureImage = default;
    static DeviceMemory textureImageMemory = default;

    // public static Vertex[] vertices = {
    //     ((-0.5f, -0.5f,0), (1.0f, 0.0f, 0.0f),(0,0)),
    //     ((0.5f, -0.5f,0), (0.0f, 1.0f, 0.0f),(1,0)),
    //     ((0.5f, 0.5f,0), (0.0f, 0.0f, 1.0f),(1,1)),
    //     ((-0.5f, 0.5f,0), (1.0f, 1.0f, 1.0f),(0,1))
    // };
    //
    // public static uint[] indices = {
    //     0, 1, 2, 2, 3, 0
    // };


    private static ImageView textureImageView;
    private static Sampler textureSampler;


    private static unsafe void AllocatePerFrameData()
    {
        FrameCleanup = (0..FRAME_OVERLAP).Times().Select((_)=> (Action)(() => { })).ToArray();
        FrameData=new FrameData[FRAME_OVERLAP];
        //command pool
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = DeviceInfo.indices.graphicsFamily!.Value,
            Flags = 0,
        };
 
        //command buffers

        //sync
        var semCreateInfo = new SemaphoreCreateInfo
        {
            SType = StructureType.SemaphoreCreateInfo,
        };
        var fenceCreateInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.SignaledBit,
        };



        fixed (FrameData* frameDatas = FrameData)
        {
            for (int i = 0; i < FRAME_OVERLAP; i++)
            {
                var z = &frameDatas[i];
            
                vk.CreateCommandPool(device, poolInfo, null,  out z->commandPool)
                    .Expect("failed to create command pool!");
                var commandbufferallocateinfo = new CommandBufferAllocateInfo()
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    CommandBufferCount = 2,
                    CommandPool = z->commandPool,
                    Level = CommandBufferLevel.Primary,
            
                };
                var commandBuffers = stackalloc CommandBuffer[2];
        
                vk.AllocateCommandBuffers(device, &commandbufferallocateinfo,commandBuffers)
                    .Expect("failed to allocate command buffers!");
                z->ComputeCommandBuffer = commandBuffers[0];
                z->GfxCommandBuffer = commandBuffers[1];
                vk.CreateSemaphore(device, semCreateInfo, null, out z->presentSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateSemaphore(device, semCreateInfo, null, out z->ComputeSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateSemaphore(device, semCreateInfo, null, out z->ComputeSemaphore2)
                    .Expect("failed to create semaphore!");
                vk.CreateSemaphore(device, semCreateInfo, null, out z->RenderSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateFence(device, fenceCreateInfo, null, out z->renderFence)
                    .Expect("failed to create fence!");
                vk.CreateFence(device,fenceCreateInfo with {Flags = 0}, null, out z->computeFence)
                    .Expect("failed to create fence!");
            }

            CleanupStack.Push(() =>
            {
                for (var i = 0; i < FRAME_OVERLAP; i++)
                {
                    vk.DestroyFence(device, FrameData[i].renderFence, null);
                    vk.DestroyFence(device, FrameData[i].computeFence, null);
                    
                    vk.DestroySemaphore(device, FrameData[i].ComputeSemaphore, null);
                    vk.DestroySemaphore(device, FrameData[i].ComputeSemaphore2, null);
                    vk.DestroySemaphore(device, FrameData[i].presentSemaphore, null);
                    vk.DestroySemaphore(device, FrameData[i].RenderSemaphore, null);
                    
                    vk.DestroyCommandPool(device, FrameData[i].commandPool, null);
                }
            });

        }
        
    }



    private static Format FindDepthFormat()
    {
        return FindSupportedFormat(new[] { Format.D32Sfloat, Format.D32SfloatS8Uint, Format.D24UnormS8Uint }, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
    }
    
    private static Format FindSupportedFormat(IEnumerable<Format> candidates, ImageTiling tiling, FormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    private static unsafe void CreateTextureSampler()
    {
        var samplerCI = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = true,
            MaxAnisotropy = 16,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = false,
            CompareEnable = false,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0,
            MinLod = 0,
            MaxLod = 0,
        };
        vk.CreateSampler(device, samplerCI, null, out textureSampler)
            .Expect("failed to create texture sampler!");
    }

    private static void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage, Format.R8G8B8A8Srgb,ImageAspectFlags.ColorBit);
    }
    
    private static unsafe ImageView CreateImageView(Image image, Format format, ImageAspectFlags imageAspectFlags)
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            SubresourceRange =
            {
                AspectMask = imageAspectFlags,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            }
        };
        
        if (vk.CreateImageView(device, createInfo, null, out var imageView) != Result.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }
    
    private static unsafe void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(AssetsPath+"/textures/texture.jpg");
        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        
        Buffer stagingBuffer = default;
        DeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize,
            BufferUsageFlags.TransferSrcBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            &stagingBuffer,
            &stagingBufferMemory);

        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        vk.UnmapMemory(device, stagingBufferMemory);

        fixed(Image* pTextureImage = &textureImage)
        fixed(DeviceMemory* pTextureImageMemory = &textureImageMemory)
            CreateImage((uint) img.Width,
            (uint) img.Height,
            Format.R8G8B8A8Srgb,
            ImageTiling.Optimal,
            ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            MemoryPropertyFlags.DeviceLocalBit,
            pTextureImage,
            pTextureImageMemory);
        

        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(textureImage, Format.R8G8B8A8Srgb, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal);

        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);


    }

    private static unsafe void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
        ImageMemoryBarrier barrier=new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            DstAccessMask = 0,// TODO
            SrcAccessMask = 0,// TODO
        };
        
        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;
        if(oldLayout==ImageLayout.Undefined&&newLayout==ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if(oldLayout==ImageLayout.TransferDstOptimal&&newLayout==ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = AccessFlags.ShaderReadBit;
            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.EarlyFragmentTestsBit;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }
        
        if(newLayout==ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.SubresourceRange.AspectMask = ImageAspectFlags.DepthBit;
            if (HasStencilComponent(format))
            {
                barrier.SubresourceRange.AspectMask |= ImageAspectFlags.StencilBit;
            }
        }
        vk.CmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        
        EndSingleTimeCommands(commandBuffer);
    }

    private static bool HasStencilComponent(Format format)
    {
        return format == Format.D32SfloatS8Uint || format == Format.D24UnormS8Uint;
    }

    private static unsafe void CopyBufferToImage(Buffer buffer, Image image, uint width, uint height)
    {
        var commandBuffer = BeginSingleTimeCommands();
        BufferImageCopy region=new()
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1,
            },
            ImageOffset = new Offset3D
            {
                X = 0,
                Y = 0,
                Z = 0,
            },
            ImageExtent = new Extent3D
            {
                Width = width,
                Height = height,
                Depth = 1,
            }
        };
        vk.CmdCopyBufferToImage(commandBuffer, buffer, image, ImageLayout.TransferDstOptimal, 1, &region);
        EndSingleTimeCommands(commandBuffer);
    }
   
    private static unsafe CommandBuffer BeginSingleTimeCommands()
    {

        CommandBuffer commandBuffer=GlobalData.oneTimeUseCommandBuffer;
        
        vk.ResetCommandBuffer(commandBuffer,CommandBufferResetFlags.None);

        CommandBufferBeginInfo beginInfo=new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        vk.BeginCommandBuffer(commandBuffer, &beginInfo);
        return commandBuffer;
    }   
    private static unsafe void EndSingleTimeCommands(CommandBuffer commandBuffer)
    {
        vk.EndCommandBuffer(commandBuffer);
        SubmitInfo submitInfo=new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &commandBuffer
        };

        vk.QueueSubmit(graphicsQueue, 1, &submitInfo, default).Expect();
        vk.QueueWaitIdle(graphicsQueue).Expect();
    }

    private static unsafe void CreateImage(uint width, uint height, Format format, ImageTiling tiling,
        ImageUsageFlags usage, MemoryPropertyFlags properties, Image* pTextureImage, DeviceMemory* pTextureImageMemory)
    {
        {
            //create image
            var imageCreateInfo = new ImageCreateInfo
            {
                SType = StructureType.ImageCreateInfo,
                ImageType = ImageType.Type2D,
                Format = format,
                Extent = new Extent3D
                {
                    Width = width,
                    Height = height,
                    Depth = 1,
                },
                MipLevels = 1,
                ArrayLayers = 1,
                Samples = SampleCountFlags.Count1Bit,
                Tiling = tiling,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Flags = 0,
            };
            vk.CreateImage(device, imageCreateInfo, null, pTextureImage)
                .Expect("failed to create image!");
        }
        MemoryRequirements memRequirements;
        vk.GetImageMemoryRequirements(device, *pTextureImage, &memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        vk.AllocateMemory(device, &allocInfo, null, pTextureImageMemory)
            .Expect("failed to allocate image memory!");

        vk.BindImageMemory(device, *pTextureImage, *pTextureImageMemory, 0);
    }

    private static unsafe void CreateDescriptorSets()
    {
        var layouts = stackalloc DescriptorSetLayout[] {DescriptorSetLayout};
        
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = DescriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = layouts,
        };
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            vk.AllocateDescriptorSets(device, &allocInfo, out FrameData[i].descriptorSets.GFX)
                .Expect("failed to allocate descriptor sets!");

            var bufferInfo = new DescriptorBufferInfo
            {
                Buffer = FrameData[i].uniformBuffer,
                Offset = 0,
                Range = (ulong) Marshal.SizeOf<UniformBufferObject>(),
            };
            var imageInfo = new DescriptorImageInfo
            {
                Sampler = textureSampler,
                ImageView = textureImageView,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            };
#pragma warning disable CA2014 //possible stack overflow
            var descriptorWrites = stackalloc WriteDescriptorSet[]
            {
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = FrameData[i].descriptorSets.GFX,
                    DstBinding = BindingPoints.GPU_Gfx_UBO,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = FrameData[i].descriptorSets.GFX,
                    DstBinding = BindingPoints.GPU_Gfx_Image_Sampler,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                },

            };
#pragma warning restore CA2014
            vk.UpdateDescriptorSets(device, 2, descriptorWrites, 0, null);
        }
    }

    private static unsafe void CreateDescriptionPool()
    {
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new (DescriptorType.UniformBuffer, FRAME_OVERLAP),
            new (DescriptorType.CombinedImageSampler, FRAME_OVERLAP),
            new (DescriptorType.StorageBuffer,FRAME_OVERLAP),//gfx
            new (DescriptorType.StorageBuffer,FRAME_OVERLAP),//compute
            new (DescriptorType.StorageBuffer,FRAME_OVERLAP),
            new (DescriptorType.StorageBuffer,FRAME_OVERLAP),
            new (DescriptorType.StorageBuffer,FRAME_OVERLAP),
            

        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 7,
            PPoolSizes = poolSizes,
            MaxSets = 7,
        };
        
        vk.CreateDescriptorPool(device, poolInfo, null, out DescriptorPool)
            .Expect("failed to create descriptor pool!");
        
    }

    private static unsafe void CreateUniformBuffers()
    {
        var bufferSize = sizeof(UniformBufferObject);
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            CreateBuffer((ulong) bufferSize,
                    BufferUsageFlags.UniformBufferBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                    out FrameData[i].uniformBuffer,
                    out FrameData[i].uniformBufferMemory);
           
    
            fixed (void** ppData = &FrameData[i].uniformBufferMapped)
                vk.MapMemory(device,FrameData[i].uniformBufferMemory, 0, (ulong) bufferSize, 0, ppData)
                    .Expect("failed to map uniform buffer memory!");
        }
        CleanupStack.Push(()=>
        {
            for (int i = 0; i < FRAME_OVERLAP; i++)
                CleanupBufferImmediately(FrameData[i].uniformBuffer, FrameData[i].uniformBufferMemory);
        });
    }

    private static unsafe void CreateDescriptorSetLayout()
    {

        var uboLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = BindingPoints.GPU_Gfx_UBO,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null,
        };
        var samplerLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = BindingPoints.GPU_Gfx_Image_Sampler,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null,
        };
        var drawcallSSBOBinding = new DescriptorSetLayoutBinding
        {
            Binding = BindingPoints.GPU_Gfx_Input_Indirect,
            DescriptorType = DescriptorType.StorageBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null,
        };
        var bindings = stackalloc[] {uboLayoutBinding, samplerLayoutBinding,drawcallSSBOBinding};
        var pBindingFlags= stackalloc DescriptorBindingFlags[]
        {
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            DescriptorBindingFlags.UpdateUnusedWhilePendingBit,
            // DescriptorBindingFlags.None
        };
        var descriptorSetLayoutBindingFlagsCreateInfo = new DescriptorSetLayoutBindingFlagsCreateInfo()
        {
            SType = StructureType.DescriptorSetLayoutBindingFlagsCreateInfo,
            BindingCount = 3,
            PBindingFlags = pBindingFlags
        };
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 3,
            PBindings = bindings,
            PNext = &descriptorSetLayoutBindingFlagsCreateInfo,
        };
        vk.CreateDescriptorSetLayout(device, layoutInfo, null, out DescriptorSetLayout)
            .Expect("failed to create descriptor set layout!");
    }

    static unsafe void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties,
        out Buffer buffer, out DeviceMemory bufferMemory)
    {
        fixed (Buffer* pBuffer = &buffer)
        fixed (DeviceMemory* pBufferMemory = &bufferMemory)
            CreateBuffer(size, usage, properties, pBuffer, pBufferMemory);
    }
    static unsafe void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, Buffer* buffer, DeviceMemory* bufferMemory)
    {
        BufferCreateInfo bufferInfo = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };
        vk.CreateBuffer(device, bufferInfo, null, buffer)
            .Expect("failed to create buffer!");
        MemoryRequirements memRequirements;
        vk.GetBufferMemoryRequirements(device, *buffer, &memRequirements);
        
        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        vk.AllocateMemory(device, &allocInfo, null, bufferMemory) 
            .Expect("failed to allocate buffer memory!");
        
        vk.BindBufferMemory(device, *buffer, *bufferMemory, 0).Expect();
    }
   

  

    static unsafe void CopyBuffer(Buffer srcBuffer, Buffer dstBuffer, ulong size)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
        BufferCopy copyRegion=new ()
        {
            SrcOffset = 0, // Optional
            DstOffset = 0, // Optional
            Size = size
        };
        vk.CmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);
        EndSingleTimeCommands(commandBuffer);
    }

    private static unsafe uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memProperties;
        vk.GetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);
        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 && memProperties.MemoryTypes[(int) i].PropertyFlags.HasFlag(properties))
            {
                return i;
            }
        }
        throw new Exception("failed to find suitable memory type!");
    }
    //
    // private static unsafe void CreateSwapchainFrameBuffers(EngineWindow window)
    // {
    //     var imageCount = window.swapChainImageViews!.Length;
    //     
    //     window.swapChainFramebuffers = new Framebuffer[imageCount];
    //     for (var i = 0; i < imageCount; i++)
    //     {
    //         var attachments = stackalloc[] {window.swapChainImageViews[i], GlobalData.depthImageView};
    //         var framebufferCreateInfo = new FramebufferCreateInfo
    //         {
    //             SType = StructureType.FramebufferCreateInfo,
    //             RenderPass = RenderPass,
    //             AttachmentCount = 2,
    //             PAttachments = attachments,
    //             Width = window.swapChainExtent.Width,
    //             Height = window.swapChainExtent.Height,
    //             Layers = 1,
    //         };
    //         vk.CreateFramebuffer(device, framebufferCreateInfo, null, out window.swapChainFramebuffers[i])
    //             .Expect("failed to create framebuffer!");
    //     }
    // }

    private static unsafe void CreateRenderPass(EngineWindow window)
    {
        //reason this needs recreation on swapchain:
        // depth format(as target),swapchainİmagheformat(as target) which never change!!!
        var subpassDependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit|PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = AccessFlags.ColorAttachmentWriteBit|AccessFlags.DepthStencilAttachmentWriteBit,
            DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit|PipelineStageFlags.EarlyFragmentTestsBit,
            DstAccessMask =
                // AccessFlags.ColorAttachmentReadBit |
                AccessFlags.ColorAttachmentWriteBit|AccessFlags.DepthStencilAttachmentWriteBit
            ,
        };
        var depthAttachment = new AttachmentDescription
        {
            Format = FindDepthFormat(),
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal,
        };
        var depthAttachmentRef = new AttachmentReference
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal,
        };
        
        var colorAttachment = new AttachmentDescription
        {
            Format = window.swapChainImageFormat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr,
        };
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal,
        };
        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef,
        };
        var attachments = stackalloc[] {colorAttachment, depthAttachment};
        var renderPassCreateInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass,
            DependencyCount = 1,
            PDependencies = &subpassDependency,
        };
        vk.CreateRenderPass(device, renderPassCreateInfo, null, out RenderPass)
            .Expect("failed to create render pass!");
    }

    private static unsafe void CreateGraphicsPipeline()
    {
        //depends on renderpass
        byte[] vertexShaderCode = File.ReadAllBytes(AssetsPath+"/shaders/compiled/triangle.vert.spv");
        byte[] fragmentShaderCode = File.ReadAllBytes(AssetsPath+"/shaders/compiled/triangle.frag.spv");
        
        var vertexModule = CreateShaderModule(vertexShaderCode);
        var fragmentModule = CreateShaderModule(fragmentShaderCode);
        
        var marshaledEntryPoint = SilkMarshal.StringToPtr("main");
        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertexModule,
            PName = (byte*) marshaledEntryPoint,
        };
        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragmentModule,
            PName = (byte*) marshaledEntryPoint
        };
        var combinedStages = stackalloc[] {fragShaderStageInfo, vertShaderStageInfo};
        
        var bindingDescription = Vertex.GetBindingDescription();
        var vertexInputAttributeDescriptions = Vertex.GetAttributeDescriptions();
        fixed (VertexInputAttributeDescription* attributeDescriptions = vertexInputAttributeDescriptions)
        {
            var vertexInputInfo = new PipelineVertexInputStateCreateInfo
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1,
                PVertexBindingDescriptions = &bindingDescription,
                VertexAttributeDescriptionCount = (uint) vertexInputAttributeDescriptions.Length,
                PVertexAttributeDescriptions = attributeDescriptions,
            };
            var inputAssembly = new PipelineInputAssemblyStateCreateInfo
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList,
                PrimitiveRestartEnable = false,
            };




            var dynamicStates = stackalloc[] {DynamicState.Viewport, DynamicState.Scissor,};

            var dynamicStateCreateInfo = new PipelineDynamicStateCreateInfo
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = dynamicStates
            };

            var viewportState = new PipelineViewportStateCreateInfo
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1,
            };

            var rasterizer = new PipelineRasterizationStateCreateInfo
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                DepthClampEnable = false,
                RasterizerDiscardEnable = false,
                PolygonMode = PolygonMode.Fill,
                LineWidth = 1,
                CullMode = CullModeFlags.BackBit,
                FrontFace = FrontFace.CounterClockwise,
                DepthBiasEnable = false,
                DepthBiasConstantFactor = 0,
                DepthBiasClamp = 0,
                DepthBiasSlopeFactor = 0,
            };

            var multiSample = new PipelineMultisampleStateCreateInfo
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                SampleShadingEnable = false,
                RasterizationSamples = SampleCountFlags.Count1Bit,
                MinSampleShading = 1,
                PSampleMask = null,
                AlphaToCoverageEnable = false,
                AlphaToOneEnable = false,
            };


            var depthStencil = new PipelineDepthStencilStateCreateInfo
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = true,
                DepthWriteEnable = true,
                DepthCompareOp = CompareOp.Less,
                DepthBoundsTestEnable = false,
                StencilTestEnable = false,
                
            };
            PipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit |
                                 ColorComponentFlags.ABit,
                BlendEnable = false,
            };

            PipelineColorBlendStateCreateInfo colorBlending = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                LogicOpEnable = false,
                LogicOp = LogicOp.Copy,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachment,
            };

            colorBlending.BlendConstants[0] = 0;
            colorBlending.BlendConstants[1] = 0;
            colorBlending.BlendConstants[2] = 0;
            colorBlending.BlendConstants[3] = 0;
            fixed (DescriptorSetLayout* pDescriptorSetLayout = &DescriptorSetLayout)
            {
                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 1,
                    PSetLayouts = pDescriptorSetLayout,
                    PushConstantRangeCount = 0,
                };

                vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out GfxPipelineLayout)
                    .Expect("failed to create pipeline layout!");
            }

        

            var GFXpipeline = new GraphicsPipelineCreateInfo
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                StageCount = 2,
                PStages = combinedStages,
                PVertexInputState = &vertexInputInfo,
                PInputAssemblyState = &inputAssembly,
                PViewportState = &viewportState,
                PRasterizationState = &rasterizer,
                PMultisampleState = &multiSample,
                PDepthStencilState = &depthStencil,
                PColorBlendState = &colorBlending,
                PDynamicState = &dynamicStateCreateInfo,
                Layout = GfxPipelineLayout,
                RenderPass = RenderPass,
                Subpass = 0,
                
                BasePipelineHandle = default,
            };
            vk.CreateGraphicsPipelines(device, default, 1, &GFXpipeline, null, out GraphicsPipeline)
                .Expect("failed to create graphics pipeline!");
        }

        vk.DestroyShaderModule(device,vertexModule,null);
        vk.DestroyShaderModule(device,fragmentModule,null);
    }

    static unsafe ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var shaderCreateInfo = new ShaderModuleCreateInfo()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) code.Length,
                PCode = (uint*) pCode,
            };
            vk.CreateShaderModule(device, shaderCreateInfo, null, out var result)
                .Expect("failed to create shader module!");
            return result;
        }

    }
    // private static unsafe void CreateSwapChainImageViews(EngineWindow window)
    // {
    //     
    //     window.swapChainImageViews = new ImageView[window.swapChainImages!.Length];
    //     for (int i = 0; i < window.swapChainImages.Length; i++)
    //     {
    //         window.swapChainImageViews[i] = CreateImageView(window.swapChainImages[i], window.swapChainImageFormat,ImageAspectFlags.ColorBit);
    //     }
    // }

    // private static unsafe void CreateSurface(EngineWindow window)
    // {
    //     if (!vk.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
    //     {
    //         throw new NotSupportedException("KHR_surface extension not found.");
    //     }
    //     
    //     window.surface = window.window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    // }


    private static unsafe void CreateInstance(byte** ppExt, int extC)
    {
        

        if (EnableValidationLayers && !CheckValidationLayerSupport())
        {
            throw new Exception("validation layers requested, but not available!");
        }

        ApplicationInfo appInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("Hello Triangle"),
            ApplicationVersion = new Version32(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
            EngineVersion = new Version32(1, 0, 0),
            ApiVersion = Vk.Version12,
        };

        InstanceCreateInfo createInfo = new();
        
        createInfo.SType = StructureType.InstanceCreateInfo;
        createInfo.PApplicationInfo = &appInfo;

        var extensions = GetRequiredInstanceExtensions(ppExt,extC);
#if MAC
        extensions = extensions.Append("VK_KHR_portability_enumeration").ToArray();
#endif
        
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

        createInfo.Flags = InstanceCreateFlags.None;
#if MAC
        createInfo.Flags |= InstanceCreateFlags.EnumeratePortabilityBitKhr;
#endif

        if (EnableValidationLayers)
        {
            createInfo.EnabledLayerCount = (uint)validationLayers.Length;
            createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);

            DebugUtilsMessengerCreateInfoEXT debugCreateInfo = new();
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.PNext = &debugCreateInfo;
        }
        else
        {
            createInfo.EnabledLayerCount = 0;
            createInfo.PNext = null;
        }

        if (vk!.CreateInstance(createInfo, null, out instance) != Result.Success)
        {
            throw new Exception("failed to create instance!");
        }
        


            

        Marshal.FreeHGlobal((IntPtr)appInfo.PApplicationName);
        Marshal.FreeHGlobal((IntPtr)appInfo.PEngineName);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        if (EnableValidationLayers)
        {
            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }
    }

    private static unsafe void SetupDebugMessenger()
    {
        if (!EnableValidationLayers) return;

        //TryGetInstanceExtension equivalent to method CreateDebugUtilsMessengerEXT from original tutorial.
        if (!vk.TryGetInstanceExtension(instance, out debugUtils!)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);
        
        if (debugUtils.CreateDebugUtilsMessenger(instance, & createInfo, null, out debugMessenger) != Result.Success)
        {
            throw new Exception("failed to set up debug messenger!");
        }
    }
    
    private static unsafe void PopulateDebugMessengerCreateInfo(ref DebugUtilsMessengerCreateInfoEXT createInfo)
    {

        createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
        createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                                     DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt;
        createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt |
                                 DebugUtilsMessageTypeFlagsEXT.ValidationBitExt;
        createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback;
    }

    private static readonly string[] requiredInstanceExtensions = {
    };
    private static unsafe string[] GetRequiredInstanceExtensions(byte** requiredWindowExtensions,int count)
    {
        
        var glfwExtensions = requiredWindowExtensions;
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)count)!
            .Concat(requiredInstanceExtensions)
        #if MAC
        .Append("VK_KHR_portability_enumeration")
        #endif
            .ToArray();
        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

    static string[] validationLayers = {"VK_LAYER_KHRONOS_validation"};
    private static unsafe bool CheckValidationLayerSupport()
    {

        uint layerCount = 0;
        vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var availableLayers = new LayerProperties[layerCount];
        fixed (LayerProperties* availableLayersPtr = availableLayers)
        {
            vk.EnumerateInstanceLayerProperties(ref layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer => Marshal.PtrToStringAnsi((IntPtr)layer.LayerName)).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }
}