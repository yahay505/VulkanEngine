using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;
using Image = Silk.NET.Vulkan.Image;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace VulkanEngine.Renderer;

public static partial class VKRender
{
    private static void InitVulkan()
    {
        CreateInstance();
        SetupDebugMessenger();
        CreateSurface();
        PickPhysicalDevice();
        // Analize
        CreateLogicalDevice();
        CreateSwapChain();
        CreateSwapChainImageViews();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        
        CreateFrameData();
        
        CreateDepthResources();
        CreateSwapchainFrameBuffers();
        CreateTextureImage();
        
        CreateTextureImageView();
        CreateTextureSampler();
        CreateVertexBuffer();
        CreateUniformBuffers();
        CreateDescriptionPool();
        CreateDescriptorSets();
        CreateIndexBuffer();
    }

    private static unsafe void CreateFrameData()
    {
        FrameData=new FrameData[FRAME_OVERLAP];
        //command pool
        var queueFamilyIndices = FindQueueFamilies(physicalDevice);
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamilyIndices.graphicsFamily!.Value,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
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
                var allocInfo = new CommandBufferAllocateInfo
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    CommandPool = z->commandPool,
                    Level = CommandBufferLevel.Primary,
                    CommandBufferCount = 1,
                };
                vk.AllocateCommandBuffers(device, allocInfo, out z->mainCommandBuffer)
                    .Expect("failed to allocate command buffers!");
                
                vk.CreateSemaphore(device, semCreateInfo, null, out z->presentSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateSemaphore(device, semCreateInfo, null, out z->transferSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateSemaphore(device, semCreateInfo, null, out z->RenderSemaphore)
                    .Expect("failed to create semaphore!");
                vk.CreateFence(device, fenceCreateInfo, null, out z->renderFence)
                    .Expect("failed to create fence!");
            }
        
        }
        
    }

    private static unsafe void CreateDepthResources()
    {
        GlobalData.depthFormat = FindDepthFormat();
        Format depthFormat = GlobalData.depthFormat;
        fixed(Image* pDepthImage = &GlobalData.depthImage)
        fixed(DeviceMemory* pDepthImageMemory= &GlobalData.depthImageMemory)
            CreateImage(swapChainExtent.Width,
                swapChainExtent.Height,
                depthFormat,
                ImageTiling.Optimal,
                ImageUsageFlags.DepthStencilAttachmentBit,
                MemoryPropertyFlags.DeviceLocalBit,
                pDepthImage,
                pDepthImageMemory);
        GlobalData.depthImageView = CreateImageView(GlobalData.depthImage, depthFormat, ImageAspectFlags.DepthBit);
        TransitionImageLayout(GlobalData.depthImage, depthFormat, ImageLayout.Undefined, ImageLayout.DepthStencilAttachmentOptimal);
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

    //todo remove
    static Image textureImage = default;
    static DeviceMemory textureImageMemory = default;
    private static unsafe void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>("../../../Assets/textures/texture.jpg");
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
        CommandBufferAllocateInfo allocInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = GetCurrentFrame().commandPool,
            CommandBufferCount = 1
        };
        CommandBuffer commandBuffer;
        vk.AllocateCommandBuffers(device, &allocInfo, &commandBuffer);
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

        vk.QueueSubmit(graphicsQueue, 1, &submitInfo, default);
        vk.QueueWaitIdle(graphicsQueue);
        vk.FreeCommandBuffers(device, GetCurrentFrame().commandPool, 1, &commandBuffer);
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
        vk.AllocateDescriptorSets(device, &allocInfo, out DescriptorSet)
            .Expect("failed to allocate descriptor sets!");
        
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            var bufferInfo = new DescriptorBufferInfo
            {
                Buffer = uniformBuffers[i],
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
                    DstSet = DescriptorSet,
                    DstBinding = 0,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.UniformBuffer,
                    DescriptorCount = 1,
                    PBufferInfo = &bufferInfo,
                },
                new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = DescriptorSet,
                    DstBinding = 1,
                    DstArrayElement = 0,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    DescriptorCount = 1,
                    PImageInfo = &imageInfo,
                }
            };
#pragma warning restore CA2014
            vk.UpdateDescriptorSets(device, 2, descriptorWrites, 0, null);
        }
    }

    private static unsafe void CreateDescriptionPool()
    {
        var poolSizes = stackalloc DescriptorPoolSize[]
        {
            new()
            {
                Type = DescriptorType.UniformBuffer,
                DescriptorCount = FRAME_OVERLAP,
            },
            new()
            {
                Type = DescriptorType.CombinedImageSampler,
                DescriptorCount = FRAME_OVERLAP,
            }
        };
        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 2,
            PPoolSizes = poolSizes,
            MaxSets = FRAME_OVERLAP,
        };
        
        vk.CreateDescriptorPool(device, poolInfo, null, out DescriptorPool)
            .Expect("failed to create descriptor pool!");
        
    }

    private static unsafe void CreateUniformBuffers()
    {
        var bufferSize = sizeof(UniformBufferObject);

        uniformBuffers=new Buffer[FRAME_OVERLAP];
        uniformBuffersMemory=new DeviceMemory[FRAME_OVERLAP];
        uniformBuffersMapped=new void*[FRAME_OVERLAP];

        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            fixed(Buffer* pUniformBuffer = &uniformBuffers[i])
            fixed(DeviceMemory* pUniformBufferMemory = &uniformBuffersMemory[i])
                CreateBuffer((ulong) bufferSize,
                    BufferUsageFlags.UniformBufferBit,
                    MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
                    pUniformBuffer,
                    pUniformBufferMemory);
            fixed (void** ppData = &uniformBuffersMapped[i])
                vk.MapMemory(device,uniformBuffersMemory[i], 0, (ulong) bufferSize, 0, ppData)
                    .Expect("failed to map uniform buffer memory!");
            
        }
    }

    private static unsafe void CreateDescriptorSetLayout()
    {
        var uboLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit,
            PImmutableSamplers = null,
        };
        var samplerLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            StageFlags = ShaderStageFlags.FragmentBit,
            PImmutableSamplers = null,
        };
        var bindings = stackalloc[] {uboLayoutBinding, samplerLayoutBinding};
        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 2,
            PBindings = bindings,
        };
        vk.CreateDescriptorSetLayout(device, layoutInfo, null, out DescriptorSetLayout)
            .Expect("failed to create descriptor set layout!");
    }

    private static Vertex[] vertices = {
        ((-0.5f, -0.5f,0), (1.0f, 0.0f, 0.0f),(0,0)),
        ((0.5f, -0.5f,0), (0.0f, 1.0f, 0.0f),(1,0)),
        ((0.5f, 0.5f,0), (0.0f, 0.0f, 1.0f),(1,1)),
        ((-0.5f, 0.5f,0), (1.0f, 1.0f, 1.0f),(0,1))
    };

    private static uint[] indices = {
        0, 1, 2, 2, 3, 0
    };

#pragma warning disable CS0649 
    //var not asssigned 
    // static Buffer vertexBuffer;
    // static DeviceMemory vertexBufferMemory;
#pragma warning restore CS0649
    static Buffer IndexBuffer;
    static DeviceMemory IndexBufferMemory;
    static Buffer[] uniformBuffers = null!;
    static DeviceMemory[] uniformBuffersMemory = null!;
    static DescriptorPool DescriptorPool;
    static DescriptorSet DescriptorSet;
    private static unsafe void*[] uniformBuffersMapped;
    private static ImageView textureImageView;
    private static Sampler textureSampler;


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
        
        vk.BindBufferMemory(device, *buffer, *bufferMemory, 0);
    }
    private static unsafe void CreateVertexBuffer()
    {
        var size = (ulong) (Marshal.SizeOf<Vertex>() * vertices.Length);
        Buffer stagingBuffer;
        DeviceMemory stagingBufferMemory;
        CreateBuffer(size, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, &stagingBuffer, &stagingBufferMemory);
        
        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, size, 0, &data)
            .Expect("failed to map vertex buffer memory!");
        fixed(void* pvertices = vertices)
            Unsafe.CopyBlock(data, pvertices, (uint)size);
        vk.UnmapMemory(device, stagingBufferMemory);
        
        
        fixed(Buffer* vertexBuffer= &GlobalData.VertexBuffer)
        fixed(DeviceMemory* vertexBufferMemory = &GlobalData.VertexBufferMemory)
            CreateBuffer(size, BufferUsageFlags.VertexBufferBit|BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, vertexBuffer, vertexBufferMemory);
        CopyBuffer(stagingBuffer, GlobalData.VertexBuffer, size);
        
        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
    }

    private static unsafe void CreateIndexBuffer()
    {
        var size = (ulong) (Marshal.SizeOf<int>() * indices.Length);
        Buffer stagingBuffer;
        DeviceMemory stagingBufferMemory;
        CreateBuffer(size, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, &stagingBuffer, &stagingBufferMemory);
        
        void* data;
        vk.MapMemory(device, stagingBufferMemory, 0, size, 0, &data)
            .Expect("failed to map index buffer memory!");
        fixed(void* pindices = indices)
            Unsafe.CopyBlock(data, pindices, (uint)size);
        vk.UnmapMemory(device, stagingBufferMemory);
        
        
        fixed(Buffer* indexBuffer= &IndexBuffer)
        fixed(DeviceMemory* indexBufferMemory = &IndexBufferMemory)
            CreateBuffer(size, BufferUsageFlags.IndexBufferBit|BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit, indexBuffer, indexBufferMemory);
        CopyBuffer(stagingBuffer, IndexBuffer, size);
        
        vk.DestroyBuffer(device, stagingBuffer, null);
        vk.FreeMemory(device, stagingBufferMemory, null);
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

    private static unsafe void RecordCommandBuffer(CommandBuffer commandBuffer, uint imageIndex)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = 0,
            PInheritanceInfo = null,
        };
        vk.BeginCommandBuffer(commandBuffer, beginInfo)
            .Expect("failed to begin recording command buffer!");

        var clearValues = stackalloc ClearValue[]
        {
            new()
            {
                Color = new (){ Float32_0 = 0, Float32_1 = 0, Float32_2 = 0, Float32_3 = 1 },
            },
            new()
            {
                DepthStencil = new () { Depth = 1, Stencil = 0 }
            }
        };
        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = RenderPass,
            Framebuffer = swapChainFramebuffers![imageIndex],
            RenderArea = new Rect2D
            {
                Offset = new Offset2D(0, 0),
                Extent = swapChainExtent,
            },
            ClearValueCount = 2,
            
            PClearValues = (clearValues)
        };
        
        var viewPort = new Viewport()
        {
            X = 0,
            Y = 0,
            Width = swapChainExtent.Width,
            Height = swapChainExtent.Height,
            MinDepth = 0,
            MaxDepth = 1,
        };
        var scissor = new Rect2D
        {
            Offset = new Offset2D(0, 0),
            Extent = swapChainExtent,
        };

        vk.CmdBeginRenderPass(commandBuffer, renderPassInfo, SubpassContents.Inline);
        vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.Graphics, GraphicsPipeline);
        vk.CmdSetViewport(commandBuffer,0,1,&viewPort);
        vk.CmdSetScissor(commandBuffer,0,1,&scissor);
        var vertexBuffers =stackalloc []{GlobalData.VertexBuffer};
        var offsets = stackalloc []{(ulong)0};
        
        // vk!.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        vk.CmdBindVertexBuffers(commandBuffer, 0, 1, vertexBuffers, offsets);
        fixed(DescriptorSet* pDescriptorSet = &DescriptorSet)
            vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.Graphics, PipelineLayout, 0, 1, pDescriptorSet, 0, null);
        vk.CmdBindIndexBuffer(commandBuffer, IndexBuffer, 0, IndexType.Uint32);
        
        vk.CmdDrawIndexed(commandBuffer, (uint) indices.Length, 2, 0, 0, 0);

        
        
        
        // vk!.CmdDraw(commandBuffer, (uint) vertices.Length, 1, 0, 0);
        vk.CmdEndRenderPass(commandBuffer);
    }

    private static unsafe void CreateSwapchainFrameBuffers()
    {
        var imageCount = swapChainImageViews!.Length;
        swapChainFramebuffers = new Framebuffer[imageCount];
        for (var i = 0; i < imageCount; i++)
        {
            var attachments = stackalloc[] {swapChainImageViews[i], GlobalData.depthImageView};
            var framebufferCreateInfo = new FramebufferCreateInfo
            {
                SType = StructureType.FramebufferCreateInfo,
                RenderPass = RenderPass,
                AttachmentCount = 2,
                PAttachments = attachments,
                Width = swapChainExtent.Width,
                Height = swapChainExtent.Height,
                Layers = 1,
            };
            vk.CreateFramebuffer(device, framebufferCreateInfo, null, out swapChainFramebuffers[i])
                .Expect("failed to create framebuffer!");
        }
    }

    private static unsafe void CreateRenderPass()
    {
        var subpassDependency = new SubpassDependency
        {
            SrcSubpass = Vk.SubpassExternal,
            DstSubpass = 0,
            SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit|PipelineStageFlags.EarlyFragmentTestsBit,
            SrcAccessMask = 0,
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
            StoreOp = AttachmentStoreOp.DontCare,
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
            Format = swapChainImageFormat,
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
        byte[] vertexShaderCode = File.ReadAllBytes("./../../../vert.spv");
        byte[] fragmentShaderCode = File.ReadAllBytes("./../../../frag.spv");
        
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

                vk.CreatePipelineLayout(device, pipelineLayoutInfo, null, out PipelineLayout)
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
                Layout = PipelineLayout,
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
    private static unsafe void CreateSwapChainImageViews()
    {
        swapChainImageViews = new ImageView[swapChainImages!.Length];
        for (int i = 0; i < swapChainImages.Length; i++)
        {
            swapChainImageViews[i] = CreateImageView(swapChainImages[i], swapChainImageFormat,ImageAspectFlags.ColorBit);
        }
    }

    private static unsafe void CreateSurface()
    {
        if (!vk.TryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
        {
            throw new NotSupportedException("KHR_surface extension not found.");
        }

        surface = window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    }


    private static unsafe void PickPhysicalDevice()
    {
        
        uint deviceCount = 0;
        vk.EnumeratePhysicalDevices(instance, &deviceCount, null);
        if (deviceCount==0)
        {
            throw new("failed to find GPUs with Vulkan support!");
        }

        var list = new PhysicalDevice[deviceCount];
        
        fixed(PhysicalDevice* n_list=list)
            vk.EnumeratePhysicalDevices(instance, &deviceCount, n_list);

        foreach (var device in list)
        {
            if (IsDeviceSuitable(device))
            {
                physicalDevice = device;
                break;
            }
        }
        if (physicalDevice.Handle==0)
        {
            throw new ("failed to find a suitable GPU!");
        }
        
        
    }
    struct QueueFamilyIndices
    {
        public uint? graphicsFamily { get; set; }
        public uint? presentFamily { get; set; }
        public uint? transferFamily { get; set; }
        public uint? computeFamily { get; set; }
        
        public bool IsComplete()
        {
            return graphicsFamily.HasValue&&
                   presentFamily.HasValue&&
                   transferFamily.HasValue&&
                   computeFamily.HasValue&&
                   true;
        }
    }

    private static unsafe QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
    {
        var indices = new QueueFamilyIndices();

        uint queueFamilityCount = 0;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, null);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilityCount];

        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilityCount, queueFamilies);
        


        
        for (uint i=0;i<queueFamilityCount;i++)
        {
            var queueFamily = queueFamilies[i];
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.graphicsFamily = i;
            }
            Bool32 presentSupport = false;
            khrSurface!.GetPhysicalDeviceSurfaceSupport(device, i, surface, &presentSupport);
            if (presentSupport)
            {
                indices.presentFamily = i;
            }
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.TransferBit))
            {
                indices.transferFamily = i;
            }
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                indices.computeFamily = i;
            }
            // queueFamily.
            if (indices.IsComplete())
            {
                break;
            }
        }
        //get device name
        var properties = vk.GetPhysicalDeviceProperties(device);
        //write all families
        Console.WriteLine($"all families for device({properties.DeviceType.ToString()}) {SilkMarshal.PtrToString((nint) properties.DeviceName)} id:{properties.DeviceID}");
        Console.WriteLine();
        for (int i = 0; i < queueFamilityCount; i++)
        {
            Console.WriteLine($"queueFamily{i}: {queueFamilies[i].QueueFlags}");
        }
        Console.WriteLine();
        Console.WriteLine("selceted families:");
        Console.WriteLine($"graphicsFamily:{indices.graphicsFamily}");
        Console.WriteLine($"presentFamily:{indices.presentFamily}");
        Console.WriteLine($"transferFamily:{indices.transferFamily}");
        Console.WriteLine($"computeFamily:{indices.computeFamily}");
        Console.WriteLine();
        return indices;
    }

    private static bool IsDeviceSuitable(PhysicalDevice device)
    {
        
        var indices = FindQueueFamilies(device);
        var features = vk.GetPhysicalDeviceFeatures(device);

        var extensionsSupported = CheckDeviceExtensionSupport(device);
        return indices.IsComplete()&& extensionsSupported&& features.SamplerAnisotropy;

        
        
        var properties = vk.GetPhysicalDeviceProperties(device);

        return
            
            // properties.DeviceType==PhysicalDeviceType.DiscreteGpu&&
            // features.GeometryShader&&
            true
            ;
    }

    private static unsafe bool CheckDeviceExtensionSupport(PhysicalDevice device)
    {
        uint extensionCount = 0;
        vk.EnumerateDeviceExtensionProperties(device, ((byte*)null)!, &extensionCount, null);
        var avaliableExtension = new ExtensionProperties[extensionCount];
        fixed (ExtensionProperties* avaliableExtensionPtr = avaliableExtension)
            vk.EnumerateDeviceExtensionProperties(device, ((byte*) null)!, ref extensionCount, avaliableExtensionPtr);

        var availableExtensionNames = avaliableExtension.Select(extension => Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName)).ToHashSet();
        return deviceExtensions.All(availableExtensionNames.Contains);    
    }

    private static unsafe void CreateInstance()
    {
        vk = Vk.GetApi();

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
            ApiVersion = Vk.Version11
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var extensions = GetRequiredExtensions();
#if MAC
        extensions = extensions.Append("VK_KHR_portability_enumeration").ToArray();
#endif

        
        
        createInfo.EnabledExtensionCount = (uint)extensions.Length;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);
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
        if (!vk.TryGetInstanceExtension(instance, out debugUtils)) return;

        DebugUtilsMessengerCreateInfoEXT createInfo = new();
        PopulateDebugMessengerCreateInfo(ref createInfo);

        if (debugUtils!.CreateDebugUtilsMessenger(instance, in createInfo, null, out debugMessenger) != Result.Success)
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

    private static unsafe string[] GetRequiredExtensions()
    {
        var glfwExtensions = window!.VkSurface!.GetRequiredExtensions(out var glfwExtensionCount);
        var extensions = SilkMarshal.PtrToStringArray((nint)glfwExtensions, (int)glfwExtensionCount);
        #if mac
        extensions = extensions!.Append("VK_KHR_portability_enumeration").ToArray();
        #endif
        if (EnableValidationLayers)
        {
            return extensions.Append(ExtDebugUtils.ExtensionName).ToArray();
        }

        return extensions;
    }

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

    private static unsafe uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT messageSeverity, DebugUtilsMessageTypeFlagsEXT messageTypes, DebugUtilsMessengerCallbackDataEXT* pCallbackData, void* pUserData)
    {
        var s = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
        Console.WriteLine($"validation layer:" + s);
        if (s.StartsWith("Validation Error:"))
        {
            ;
        }
//Debugger.Break();
        return Vk.False;
    }
}