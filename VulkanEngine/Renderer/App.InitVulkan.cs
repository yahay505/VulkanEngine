using System.Runtime.InteropServices;
using Cathei.LinqGen;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Vulkan;
using VulkanEngine.Renderer.GPUStructs;
using static Vortice.Vulkan.Vulkan;

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


    public static unsafe void InitVulkanFirstPhase()
    {
        CreateVkInstance();
    }
    public static void InitVulkanSecondPhase(VkSurfaceKHR surface)
    {
        DeviceInfo=DeviceRequirements.PickPhysicalDevice(surface);
        CreateLogicalDevice();
        AllocateGlobalData();

    }

    //todo remove
    static VkImage textureImage = default;
    static VkDeviceMemory textureImageMemory = default;

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


    private static VkImageView textureImageView;
    private static VkSampler textureSampler;


    private static unsafe void AllocatePerFrameData()
    {
        FrameCleanup = (0..FRAME_OVERLAP).Times().Select((_)=> (Action)(() => { })).ToArray();
        FrameData=new FrameData[FRAME_OVERLAP];
        //command pool
        var poolInfo = new VkCommandPoolCreateInfo
       {
            queueFamilyIndex = DeviceInfo.indices.graphicsFamily!.Value,
            flags = 0,
        };
 
        //command buffers

        //sync
        var semCreateInfo = new VkSemaphoreCreateInfo
       {
        };



        fixed (FrameData* frameDatas = FrameData)
        {
            for (int i = 0; i < FRAME_OVERLAP; i++)
            {
                var fenceCreateInfo = new VkFenceCreateInfo
                {
                    flags = VkFenceCreateFlags.Signaled,
                    pNext = null,
                };

                var z = &frameDatas[i];
            
                vkCreateCommandPool(device, &poolInfo, null,  out z->commandPool)
                    .Expect("failed to create command pool!");
                var commandbufferallocateinfo = new VkCommandBufferAllocateInfo()
               {
                    commandBufferCount = 2,
                    commandPool = z->commandPool,
                    level = VkCommandBufferLevel.Primary,
            
                };
                var commandBuffers = stackalloc VkCommandBuffer[2];
        
                vkAllocateCommandBuffers(device, &commandbufferallocateinfo,commandBuffers)
                    .Expect("failed to allocate command buffers!");
                z->ComputeCommandBuffer = commandBuffers[0];
                z->GfxCommandBuffer = commandBuffers[1];
                vkCreateSemaphore(device, &semCreateInfo, null, out z->presentSemaphore)
                    .Expect("failed to create semaphore!");
                vkCreateSemaphore(device, &semCreateInfo, null, out z->ComputeSemaphore)
                    .Expect("failed to create semaphore!");
                vkCreateSemaphore(device, &semCreateInfo, null, out z->ComputeSemaphore2)
                    .Expect("failed to create semaphore!");
                vkCreateSemaphore(device, &semCreateInfo, null, out z->RenderSemaphore)
                    .Expect("failed to create semaphore!");
                vkCreateFence(device, &fenceCreateInfo, null, out z->renderFence)
                    .Expect("failed to create fence!");

                fenceCreateInfo = fenceCreateInfo with {flags = 0};
                vkCreateFence(device, &fenceCreateInfo, null, out z->computeFence)
                    .Expect("failed to create fence!");

            }

            CleanupStack.Push(() =>
            {
                for (var i = 0; i < FRAME_OVERLAP; i++)
                {
                    vkDestroyFence(device, FrameData[i].renderFence, null);
                    vkDestroyFence(device, FrameData[i].computeFence, null);
                    
                    vkDestroySemaphore(device, FrameData[i].ComputeSemaphore, null);
                    vkDestroySemaphore(device, FrameData[i].ComputeSemaphore2, null);
                    vkDestroySemaphore(device, FrameData[i].presentSemaphore, null);
                    vkDestroySemaphore(device, FrameData[i].RenderSemaphore, null);
                    
                    vkDestroyCommandPool(device, FrameData[i].commandPool, null);
                }
            });

        }
        
    }



    private static VkFormat FindDepthFormat()
    {
        return FindSupportedFormat(new[] { VkFormat.D32Sfloat, VkFormat.D32SfloatS8Uint, VkFormat.D24UnormS8Uint }, VkImageTiling.Optimal, VkFormatFeatureFlags.DepthStencilAttachment);
    }
    
    private static VkFormat FindSupportedFormat(VkFormat[] candidates, VkImageTiling tiling, VkFormatFeatureFlags features)
    {
        foreach (var format in candidates)
        {
            vkGetPhysicalDeviceFormatProperties(physicalDevice, format, out var props);

            if (tiling == VkImageTiling.Linear && (props.linearTilingFeatures & features) == features)
            {
                return format;
            }
            else if (tiling == VkImageTiling.Optimal && (props.optimalTilingFeatures & features) == features)
            {
                return format;
            }
        }

        throw new Exception("failed to find supported format!");
    }

    private static unsafe void CreateTextureSampler()
    {
        var samplerCI = new VkSamplerCreateInfo
       {
            magFilter = VkFilter.Linear,
            minFilter = VkFilter.Linear,
            addressModeU = VkSamplerAddressMode.Repeat,
            addressModeV = VkSamplerAddressMode.Repeat,
            addressModeW = VkSamplerAddressMode.Repeat,
            anisotropyEnable = true,
            maxAnisotropy = 16,
            borderColor = VkBorderColor.IntOpaqueBlack,
            unnormalizedCoordinates = false,
            compareEnable = false,
            compareOp = VkCompareOp.Always,
            mipmapMode = VkSamplerMipmapMode.Linear,
            mipLodBias = 0,
            minLod = 0,
            maxLod = 0,
        };
        vkCreateSampler(device, &samplerCI, null, out textureSampler)
            .Expect("failed to create texture sampler!");
    }

    private static void CreateTextureImageView()
    {
        textureImageView = CreateImageView(textureImage, VkFormat.R8G8B8A8Srgb,VkImageAspectFlags.Color);
    }
    
    private static unsafe VkImageView CreateImageView(VkImage image, VkFormat format, VkImageAspectFlags imageAspectFlags)
    {
        VkImageViewCreateInfo createInfo = new()
       {
            image = image,
            viewType = VkImageViewType.Image2D,
            format = format,
            //Components =
            //    {
            //        R = ComponentSwizzle.Identity,
            //        G = ComponentSwizzle.Identity,
            //        B = ComponentSwizzle.Identity,
            //        A = ComponentSwizzle.Identity,
            //    },
            subresourceRange =
            {
                aspectMask = imageAspectFlags,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1,
            }
        };
        
        if (vkCreateImageView(device, &createInfo, null, out var imageView) != VkResult.Success)
        {
            throw new Exception("failed to create image views!");
        }

        return imageView;
    }
    
    private static unsafe void CreateTextureImage()
    {
        using var img = SixLabors.ImageSharp.Image.Load<Rgba32>(AssetsPath+"/textures/texture.jpg");
        ulong imageSize = (ulong)(img.Width * img.Height * img.PixelType.BitsPerPixel / 8);
        
        VkBuffer stagingBuffer = default;
        VkDeviceMemory stagingBufferMemory = default;
        CreateBuffer(imageSize,
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
            &stagingBuffer,
            &stagingBufferMemory);

        void* data;
        vkMapMemory(device, stagingBufferMemory, 0, imageSize, 0, &data);
        img.CopyPixelDataTo(new Span<byte>(data, (int)imageSize));
        vkUnmapMemory(device, stagingBufferMemory);

        fixed(VkImage* pTextureImage = &textureImage)
        fixed(VkDeviceMemory* pTextureImageMemory = &textureImageMemory)
            CreateImage((uint) img.Width,
            (uint) img.Height,
            VkFormat.R8G8B8A8Srgb,
            VkImageTiling.Optimal,
            VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
            VkMemoryPropertyFlags.DeviceLocal,
            pTextureImage,
            pTextureImageMemory);
        

        TransitionImageLayout(textureImage, VkFormat.R8G8B8A8Srgb, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
        CopyBufferToImage(stagingBuffer, textureImage, (uint)img.Width, (uint)img.Height);
        TransitionImageLayout(textureImage, VkFormat.R8G8B8A8Srgb, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

        vkDestroyBuffer(device, stagingBuffer, null);
        vkFreeMemory(device, stagingBufferMemory, null);


    }

    private static unsafe void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
        VkImageMemoryBarrier barrier=new()
       {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            dstAccessMask = 0,// TODO
            srcAccessMask = 0,// TODO
        };
        
        VkPipelineStageFlags sourceStage;
        VkPipelineStageFlags destinationStage;
        if(oldLayout==VkImageLayout.Undefined&&newLayout==VkImageLayout.TransferDstOptimal)
        {
            barrier.srcAccessMask = 0;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;
            sourceStage = VkPipelineStageFlags.TopOfPipe;
            destinationStage = VkPipelineStageFlags.Transfer;
        }
        else if(oldLayout==VkImageLayout.TransferDstOptimal&&newLayout==VkImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;
            sourceStage = VkPipelineStageFlags.Transfer;
            destinationStage = VkPipelineStageFlags.FragmentShader;
        }
        else if (oldLayout == VkImageLayout.Undefined && newLayout == VkImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.srcAccessMask = 0;
            barrier.dstAccessMask = VkAccessFlags.DepthStencilAttachmentRead | VkAccessFlags.DepthStencilAttachmentWrite;
            sourceStage = VkPipelineStageFlags.TopOfPipe;
            destinationStage = VkPipelineStageFlags.EarlyFragmentTests;
        }
        else
        {
            throw new Exception("unsupported layout transition!");
        }
        
        if(newLayout==VkImageLayout.DepthStencilAttachmentOptimal)
        {
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Depth;
            if (HasStencilComponent(format))
            {
                barrier.subresourceRange.aspectMask |= VkImageAspectFlags.Stencil;
            }
        }
        vkCmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );
        
        
        EndSingleTimeCommands(commandBuffer);
    }

    private static bool HasStencilComponent(VkFormat format)
    {
        return format == VkFormat.D32SfloatS8Uint || format == VkFormat.D24UnormS8Uint;
    }

    private static unsafe void CopyBufferToImage(VkBuffer buffer, VkImage image, uint width, uint height)
    {
        var commandBuffer = BeginSingleTimeCommands();
        VkBufferImageCopy region=new()
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            imageOffset = new VkOffset3D
            {
                x = 0,
                y = 0,
                z = 0,
            },
            imageExtent = new VkExtent3D
            {
                width = width,
                height = height,
                depth = 1,
            }
        };
        vkCmdCopyBufferToImage(commandBuffer, buffer, image, VkImageLayout.TransferDstOptimal, 1, &region);
        EndSingleTimeCommands(commandBuffer);
    }
   
    private static unsafe VkCommandBuffer BeginSingleTimeCommands()
   {
        VkCommandBuffer commandBuffer=GlobalData.oneTimeUseCommandBuffer;
        
        vkResetCommandBuffer(commandBuffer,VkCommandBufferResetFlags.None);

        VkCommandBufferBeginInfo beginInfo=new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };

        vkBeginCommandBuffer(commandBuffer, &beginInfo);
        return commandBuffer;
    }   
    private static unsafe void EndSingleTimeCommands(VkCommandBuffer commandBuffer)
    {
        vkEndCommandBuffer(commandBuffer);
        VkSubmitInfo submitInfo=new()
       {
            commandBufferCount = 1,
            pCommandBuffers = &commandBuffer
        };

        vkQueueSubmit(graphicsQueue, 1, &submitInfo, default).Expect();
        vkQueueWaitIdle(graphicsQueue).Expect();
    }

    private static unsafe void CreateImage(uint width, uint height, VkFormat format, VkImageTiling tiling,
        VkImageUsageFlags usage, VkMemoryPropertyFlags properties, VkImage* pTextureImage, VkDeviceMemory* pTextureImageMemory)
    {
        {
            //create image
            var imageCreateInfo = new VkImageCreateInfo
           {
                imageType = VkImageType.Image2D,
                format = format,
                extent = new VkExtent3D
                {
                    width = width,
                    height = height,
                    depth = 1,
                },
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = tiling,
                usage = usage,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined,
                flags = 0,
            };
            vkCreateImage(device, &imageCreateInfo, null, pTextureImage)
                .Expect("failed to create image!");
        }
        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(device, *pTextureImage, &memRequirements);

        VkMemoryAllocateInfo allocInfo = new()
       {
            allocationSize = memRequirements.size,
            memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties)
        };

        vkAllocateMemory(device, &allocInfo, null, pTextureImageMemory)
            .Expect("failed to allocate image memory!");

        vkBindImageMemory(device, *pTextureImage, *pTextureImageMemory, 0);
    }

    private static unsafe void CreateDescriptorSets()
    {
        var layouts = stackalloc VkDescriptorSetLayout[] {DescriptorSetLayout};
        
        var allocInfo = new VkDescriptorSetAllocateInfo
       {
            descriptorPool = DescriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = layouts,
        };
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            VkDescriptorSet a;
            vkAllocateDescriptorSets(device, &allocInfo, &a)
                .Expect("failed to allocate descriptor sets!");
            FrameData[i].descriptorSets.GFX = a;
            var bufferInfo = new VkDescriptorBufferInfo
            {
                buffer = FrameData[i].uniformBuffer,
                offset = 0,
                range = (ulong) Marshal.SizeOf<UniformBufferObject>(),
            };
            var imageInfo = new VkDescriptorImageInfo
            {
                sampler = textureSampler,
                imageView = textureImageView,
                imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            };
#pragma warning disable CA2014 //possible stack overflow
            var descriptorWrites = stackalloc VkWriteDescriptorSet[]
            {
                new()
               {
                    dstSet = FrameData[i].descriptorSets.GFX,
                    dstBinding = BindingPoints.GPU_Gfx_UBO,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.UniformBuffer,
                    descriptorCount = 1,
                    pBufferInfo = &bufferInfo,
                },
                new()
               {
                    dstSet = FrameData[i].descriptorSets.GFX,
                    dstBinding = BindingPoints.GPU_Gfx_Image_Sampler,
                    dstArrayElement = 0,
                    descriptorType = VkDescriptorType.CombinedImageSampler,
                    descriptorCount = 1,
                    pImageInfo = &imageInfo,
                },

            };
#pragma warning restore CA2014
            vkUpdateDescriptorSets(device, 2, descriptorWrites, 0, null);
        }
    }

    private static unsafe void CreateDescriptionPool()
    {
        var poolSizes = stackalloc VkDescriptorPoolSize[]
        {
            new (VkDescriptorType.UniformBuffer, FRAME_OVERLAP),
            new (VkDescriptorType.CombinedImageSampler, FRAME_OVERLAP),
            new (VkDescriptorType.StorageBuffer,FRAME_OVERLAP),//gfx
            new (VkDescriptorType.StorageBuffer,FRAME_OVERLAP),//compute
            new (VkDescriptorType.StorageBuffer,FRAME_OVERLAP),
            new (VkDescriptorType.StorageBuffer,FRAME_OVERLAP),
            new (VkDescriptorType.StorageBuffer,FRAME_OVERLAP),
            

        };
        var poolInfo = new VkDescriptorPoolCreateInfo
       {
            poolSizeCount = 7,
            pPoolSizes = poolSizes,
            maxSets = 7,
        };
        VkDescriptorPool b;
        vkCreateDescriptorPool(device, &poolInfo, null, & b)
            .Expect("failed to create descriptor pool!");
        DescriptorPool = b;
    }

    private static unsafe void CreateUniformBuffers()
    {
        var bufferSize = sizeof(UniformBufferObject);
        for (int i = 0; i < FRAME_OVERLAP; i++)
        {
            CreateBuffer((ulong) bufferSize,
                    VkBufferUsageFlags.UniformBuffer,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                    out FrameData[i].uniformBuffer,
                    out FrameData[i].uniformBufferMemory);
           
    
            fixed (void** ppData = &FrameData[i].uniformBufferMapped)
                vkMapMemory(device,FrameData[i].uniformBufferMemory, 0, (ulong) bufferSize, 0, ppData)
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
        var uboLayoutBinding = new VkDescriptorSetLayoutBinding
        {
            binding = BindingPoints.GPU_Gfx_UBO,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex,
            pImmutableSamplers = null,
        };
        var samplerLayoutBinding = new VkDescriptorSetLayoutBinding
        {
            binding = BindingPoints.GPU_Gfx_Image_Sampler,
            descriptorCount = 1,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            stageFlags = VkShaderStageFlags.Fragment,
            pImmutableSamplers = null,
        };
        var drawcallSSBOBinding = new VkDescriptorSetLayoutBinding
        {
            binding = BindingPoints.GPU_Gfx_Input_Indirect,
            descriptorType = VkDescriptorType.StorageBuffer,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.Vertex,
            pImmutableSamplers = null,
        };
        var bindings = stackalloc[] {uboLayoutBinding, samplerLayoutBinding,drawcallSSBOBinding};
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
        var layoutInfo = new VkDescriptorSetLayoutCreateInfo
       {
            bindingCount = 3,
            pBindings = bindings,
            pNext = &descriptorSetLayoutBindingFlagsCreateInfo,
        };
        vkCreateDescriptorSetLayout(device, &layoutInfo, null, out DescriptorSetLayout)
            .Expect("failed to create descriptor set layout!");
    }

    static unsafe void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties,
        out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        fixed (VkBuffer* pBuffer = &buffer)
        fixed (VkDeviceMemory* pBufferMemory = &bufferMemory)
            CreateBuffer(size, usage, properties, pBuffer, pBufferMemory);
    }
    static unsafe void CreateBuffer(ulong size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, VkBuffer* buffer, VkDeviceMemory* bufferMemory)
    {
        VkBufferCreateInfo bufferInfo = new()
       {
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };
        vkCreateBuffer(device, &bufferInfo, null, buffer)
            .Expect("failed to create buffer!");
        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(device, *buffer, &memRequirements);
        
        VkMemoryAllocateInfo allocInfo = new()
       {
            allocationSize = memRequirements.size,
            memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties)
        };

        vkAllocateMemory(device, &allocInfo, null, bufferMemory) 
            .Expect("failed to allocate buffer memory!");
        
        vkBindBufferMemory(device, *buffer, *bufferMemory, 0).Expect();
    }
   

  

    static unsafe void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size)
    {
        var commandBuffer = BeginSingleTimeCommands();
        
        VkBufferCopy copyRegion=new ()
        {
            srcOffset = 0, // Optional
            dstOffset = 0, // Optional
            size = size
        };
        vkCmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);
        EndSingleTimeCommands(commandBuffer);
    }

    private static unsafe uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        vkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);
        for (uint i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 && memProperties.memoryTypes[(int) i].propertyFlags.HasFlag(properties))
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
    //        {
    //             RenderPass = RenderPass,
    //             AttachmentCount = 2,
    //             PAttachments = attachments,
    //             Width = window.swapChainExtent.Width,
    //             Height = window.swapChainExtent.Height,
    //             Layers = 1,
    //         };
    //         vkCreateFramebuffer(device, framebufferCreateInfo, null, out window.swapChainFramebuffers[i])
    //             .Expect("failed to create framebuffer!");
    //     }
    // }

    private static unsafe void CreateRenderPass(EngineWindow window)
    {
        //reason this needs recreation on swapchain:
        // depth format(as target),swapchainİmagheformat(as target) which never change!!!
        var subpassDependency = new VkSubpassDependency
        {
            srcSubpass = VK_SUBPASS_EXTERNAL,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput|VkPipelineStageFlags.EarlyFragmentTests,
            srcAccessMask = VkAccessFlags.ColorAttachmentWrite|VkAccessFlags.DepthStencilAttachmentWrite,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput|VkPipelineStageFlags.EarlyFragmentTests,
            dstAccessMask =
                // VkAccessFlags.ColorAttachmentRead |
                VkAccessFlags.ColorAttachmentWrite|VkAccessFlags.DepthStencilAttachmentWrite
            ,
        };
        var depthAttachment = new VkAttachmentDescription
        {
            format = FindDepthFormat(),
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.DepthStencilAttachmentOptimal,
        };
        var depthAttachmentRef = new VkAttachmentReference
        {
            attachment = 1,
            layout = VkImageLayout.DepthStencilAttachmentOptimal,
        };
        
        var colorAttachment = new VkAttachmentDescription
        {
            format = window.swapChainImageFormat,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR,
        };
        var colorAttachmentRef = new VkAttachmentReference
        {
            attachment = 0,
            layout = VkImageLayout.ColorAttachmentOptimal,
        };
        var subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentRef,
            pDepthStencilAttachment = &depthAttachmentRef,
        };
        var attachments = stackalloc[] {colorAttachment, depthAttachment};
        var renderPassCreateInfo = new VkRenderPassCreateInfo
       {
            attachmentCount = 2,
            pAttachments = attachments,
            subpassCount = 1,
            pSubpasses = &subpass,
            dependencyCount = 1,
            pDependencies = &subpassDependency,
        };
        vkCreateRenderPass(device, &renderPassCreateInfo, null, out RenderPass)
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
        var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo
       {
            stage = VkShaderStageFlags.Vertex,
            module = vertexModule,
            pName = (sbyte*) marshaledEntryPoint,
        };
        var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo
        {

            stage = VkShaderStageFlags.Fragment,
            module = fragmentModule,
            pName = (sbyte*) marshaledEntryPoint
        };
        var combinedStages = stackalloc[] {fragShaderStageInfo, vertShaderStageInfo};
        
        var bindingDescription = Vertex.GetBindingDescription();
        var vertexInputAttributeDescriptions = Vertex.GetAttributeDescriptions();
        fixed (VkVertexInputAttributeDescription* attributeDescriptions = vertexInputAttributeDescriptions)
        {
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {

                vertexBindingDescriptionCount = 1,
                pVertexBindingDescriptions = &bindingDescription,
                vertexAttributeDescriptionCount = (uint) vertexInputAttributeDescriptions.Length,
                pVertexAttributeDescriptions = attributeDescriptions,
            };
            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
            {

                topology = VkPrimitiveTopology.TriangleList,
                primitiveRestartEnable = false,
            };




            var dynamicStates = stackalloc[] {VkDynamicState.Viewport, VkDynamicState.Scissor,};

            var dynamicStateCreateInfo = new VkPipelineDynamicStateCreateInfo
            {
                dynamicStateCount = 2,
                pDynamicStates = dynamicStates
            };

            var viewportState = new VkPipelineViewportStateCreateInfo
            {

                viewportCount = 1,
                scissorCount = 1,
            };

            var rasterizer = new VkPipelineRasterizationStateCreateInfo
            {
                depthClampEnable = false,
                rasterizerDiscardEnable = false,
                polygonMode = VkPolygonMode.Fill,
                lineWidth = 1,
                cullMode = VkCullModeFlags.Back,
                frontFace = VkFrontFace.CounterClockwise,
                depthBiasEnable = false,
                depthBiasConstantFactor = 0,
                depthBiasClamp = 0,
                depthBiasSlopeFactor = 0,
            };

            var multiSample = new VkPipelineMultisampleStateCreateInfo
            {

                sampleShadingEnable = false,
                rasterizationSamples = VkSampleCountFlags.Count1,
                minSampleShading = 1,
                pSampleMask = null,
                alphaToCoverageEnable = false,
                alphaToOneEnable = false,
            };


            var depthStencil = new VkPipelineDepthStencilStateCreateInfo
            {

                depthTestEnable = true,
                depthWriteEnable = true,
                depthCompareOp = VkCompareOp.Less,
                depthBoundsTestEnable = false,
                stencilTestEnable = false,
                
            };
            VkPipelineColorBlendAttachmentState colorBlendAttachment = new()
            {
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B |
                                 VkColorComponentFlags.A,
                blendEnable = false,
            };

            VkPipelineColorBlendStateCreateInfo colorBlending = new()
            {

                logicOpEnable = false,
                logicOp = VkLogicOp.Copy,
                attachmentCount = 1,
                pAttachments = &colorBlendAttachment,
            };

            colorBlending.blendConstants[0] = 0;
            colorBlending.blendConstants[1] = 0;
            colorBlending.blendConstants[2] = 0;
            colorBlending.blendConstants[3] = 0;
            fixed (VkDescriptorSetLayout* pDescriptorSetLayout = &DescriptorSetLayout)
            {
                VkPipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {

                    setLayoutCount = 1,
                    pSetLayouts = pDescriptorSetLayout,
                    pushConstantRangeCount = 0,
                };

                vkCreatePipelineLayout(device, &pipelineLayoutInfo, null, out GfxPipelineLayout)
                    .Expect("failed to create pipeline layout!");
            }

        

            var GFXpipeline = new VkGraphicsPipelineCreateInfo
            {

                stageCount = 2,
                pStages = combinedStages,
                pVertexInputState = &vertexInputInfo,
                pInputAssemblyState = &inputAssembly,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizer,
                pMultisampleState = &multiSample,
                pDepthStencilState = &depthStencil,
                pColorBlendState = &colorBlending,
                pDynamicState = &dynamicStateCreateInfo,
                layout = GfxPipelineLayout,
                renderPass = RenderPass,
                subpass = 0,
                
                basePipelineHandle = default,
            };
            VkPipeline p;
            vkCreateGraphicsPipelines(device, default, 1, &GFXpipeline, null, &p)
                .Expect("failed to create graphics pipeline!");
            GraphicsPipeline = p;
        }

        vkDestroyShaderModule(device,vertexModule,null);
        vkDestroyShaderModule(device,fragmentModule,null);
    }

    static unsafe VkShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* pCode = code)
        {
            var shaderCreateInfo = new VkShaderModuleCreateInfo()
            {
                codeSize = (nuint) code.Length,
                pCode = (uint*) pCode,
            };
            vkCreateShaderModule(device, &shaderCreateInfo, null, out var result)
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
    //         window.swapChainImageViews[i] = CreateImageView(window.swapChainImages[i], window.swapChainImageFormat,ImageAspectFlags.Color);
    //     }
    // }

    // private static unsafe void CreateSurface(EngineWindow window)
    // {
    //     if (!vkTryGetInstanceExtension<KhrSurface>(instance, out khrSurface))
    //     {
    //         throw new NotSupportedException("KHR_surface extension not found.");
    //     }
    //     
    //     window.surface = window.window!.VkSurface!.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
    // }


    private static unsafe void CreateVkInstance()
    {
        var applicationInfo = new VkApplicationInfo()
            {
                //sType = VkStructureType.ApplicationInfo,
                apiVersion = new VkVersion(1, 2, 0),
                pEngineName = (sbyte*)Marshal.StringToHGlobalAnsi("Vengine"),
                //engineVersion = new VkVersion()
            };

            var validationLayerNames = new[]
            {
                "VK_LAYER_KHRONOS_validation",
            };

            IntPtr[] enabledLayerNames = new IntPtr[0];

            if (EnableValidationLayers)
            {
                var layers = vkEnumerateInstanceLayerProperties();
                var availableLayerNames = new HashSet<string>();

                for (int index = 0; index < layers.Length; index++)
                {
                    var properties = layers[index];
                    var namePointer = properties.layerName;
                    var name = Marshal.PtrToStringAnsi((IntPtr)namePointer);

                    availableLayerNames.Add(name);
                }

                enabledLayerNames = validationLayerNames
                    .Where(x => availableLayerNames.Contains(x))
                    .Select(Marshal.StringToHGlobalAnsi).ToArray();

                // Check if validation was really available
                
                Console.WriteLine($"Enabled Validation Layers: {enabledLayerNames.Length > 0}");
            }

            var extensionProperties = vkEnumerateInstanceExtensionProperties();
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var extensionProperty = extensionProperties[index];
                var name = Marshal.PtrToStringAnsi((IntPtr)extensionProperty.extensionName);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add(VK_KHR_SURFACE_EXTENSION_NAME);
            if (!availableExtensionNames.Contains(VK_KHR_SURFACE_EXTENSION_NAME))
                throw new InvalidOperationException($"Required extension {VK_KHR_SURFACE_EXTENSION_NAME} is not available");

            if (MIT.OS == OSType.Windows)
            {
                desiredExtensionNames.Add(KHRWin32SurfaceExtensionName);
                if (!availableExtensionNames.Contains(KHRWin32SurfaceExtensionName))
                    throw new InvalidOperationException($"Required extension {KHRWin32SurfaceExtensionName} is not available");
            }
            else if (MIT.OS == OSType.Mac)
            {
                desiredExtensionNames.Add(VK_MVK_MACOS_SURFACE_EXTENSION_NAME);
                desiredExtensionNames.Add(VK_EXT_METAL_SURFACE_EXTENSION_NAME);
                desiredExtensionNames.Add("VK_KHR_portability_enumeration");
                if (!availableExtensionNames.Contains(VK_MVK_MACOS_SURFACE_EXTENSION_NAME) || !availableExtensionNames.Contains(VK_EXT_METAL_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {VK_MVK_MACOS_SURFACE_EXTENSION_NAME} or {VK_EXT_METAL_SURFACE_EXTENSION_NAME} is not available");
            }
            else if (MIT.OS == OSType.Android)
            {
                desiredExtensionNames.Add(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME);
                if (!availableExtensionNames.Contains(VK_KHR_ANDROID_SURFACE_EXTENSION_NAME))
                    throw new InvalidOperationException($"Required extension {VK_KHR_ANDROID_SURFACE_EXTENSION_NAME} is not available");
            }
            else if (MIT.OS == OSType.Linux)
            {
                // if (availableExtensionNames.Contains("VK_KHR_xlib_surface"))
                // {
                //     desiredExtensionNames.Add("VK_KHR_xlib_surface");
                //     HasXlibSurfaceSupport = true;
                // }
                // else if (availableExtensionNames.Contains("VK_KHR_xcb_surface"))
                // {
                //     desiredExtensionNames.Add("VK_KHR_xcb_surface");
                // }
                // else
                // {
                //     throw new InvalidOperationException("None of the supported surface extensions VK_KHR_xcb_surface or VK_KHR_xlib_surface is available");
                // }
            }

            bool enableDebugReport = EnableValidationLayers && availableExtensionNames.Contains(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
            if (enableDebugReport)
                desiredExtensionNames.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                fixed (void* enabledExtensionNamesPointer = &enabledExtensionNames[0])
                fixed (void* fEnabledLayerNames = enabledLayerNames) // null if array is empty or null
                {
                    VkDebugUtilsMessengerCreateInfoEXT a = new()
                    {
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning| VkDebugUtilsMessageSeverityFlagsEXT.Info| VkDebugUtilsMessageSeverityFlagsEXT.Verbose,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance| VkDebugUtilsMessageTypeFlagsEXT.DeviceAddressBinding,
                        pfnUserCallback = &DebugCallback
                        
                    };
                    
                    var instanceCreateInfo = new VkInstanceCreateInfo()
                    {
                        pApplicationInfo = &applicationInfo,
                        enabledLayerCount = enabledLayerNames != null ? (uint)enabledLayerNames.Length : 0,
                        ppEnabledLayerNames = (sbyte**)fEnabledLayerNames,
                        enabledExtensionCount = (uint)enabledExtensionNames.Length,
                        ppEnabledExtensionNames = (sbyte**)enabledExtensionNamesPointer,
                        flags = MIT.OS==OSType.Mac?VkInstanceCreateFlags.EnumeratePortabilityKHR:VkInstanceCreateFlags.None ,
                        pNext = EnableValidationLayers?&a:default,
                    };

                    vkCreateInstance(&instanceCreateInfo, null, out instance);
                    vkLoadInstance(instance);
                }

                // Check if validation layer was available (otherwise detected count is 0)
                if (EnableValidationLayers)
                {
                    var createInfo = new VkDebugUtilsMessengerCreateInfoEXT()
                    {
                        messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose | VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning| VkDebugUtilsMessageSeverityFlagsEXT.Info| VkDebugUtilsMessageSeverityFlagsEXT.Verbose,
                        messageType = VkDebugUtilsMessageTypeFlagsEXT.General | VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance| VkDebugUtilsMessageTypeFlagsEXT.DeviceAddressBinding ,
                        pfnUserCallback = &DebugCallback
                    };
                    

                    vkCreateDebugUtilsMessengerEXT(instance, &createInfo, null, out var debugReportCallback).CheckResult();
                }
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }

                foreach (var enabledLayerName in enabledLayerNames)
                {
                    Marshal.FreeHGlobal(enabledLayerName);
                }

                Marshal.FreeHGlobal((IntPtr)applicationInfo.pEngineName);
            }
    }


    
    private static readonly string[] requiredInstanceExtensions = {
    };
    private static unsafe string[] GetRequiredInstanceExtensions()
    {


        var extensions = MIT.VulkanWindowingInstanceExtensions().ToArray().Concat(requiredInstanceExtensions)
#if MAC
                .Append("VK_KHR_portability_enumeration")
#endif
            ;
        if (EnableValidationLayers)
        {
            return extensions.Append(VK_EXT_DEBUG_UTILS_EXTENSION_NAME).ToArray();
        }

        return extensions.ToArray();
    }

    static string[] validationLayers = {"VK_LAYER_KHRONOS_validation"};
    private static unsafe bool CheckValidationLayerSupport()
    {

        uint layerCount = 0;
        vkEnumerateInstanceLayerProperties(&layerCount, null).Expect();
        var availableLayers = new VkLayerProperties[layerCount];
        fixed (VkLayerProperties* availableLayersPtr = availableLayers)
        {
            vkEnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);
        }

        var availableLayerNames = availableLayers.Select(layer =>
        {
            return Marshal.PtrToStringAnsi((nint) layer.layerName);
        }).ToHashSet();

        return validationLayers.All(availableLayerNames.Contains);
    }
}