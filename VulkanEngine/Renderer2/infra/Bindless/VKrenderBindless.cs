using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static VulkanEngine.Renderer2.infra.GPUDEBUG;

namespace VulkanEngine.Renderer2.infra.Bindless;

public static class TextureManager
{
    public static VkDescriptorSet descriptorset;
    private static VkDescriptorPool descpool;
    public static VkDescriptorSetLayout descSetLayout;
    private static Dictionary<string,EngineImage> textures = new();

    internal static unsafe void InitTextureEngine()
    {
         (descriptorset, descpool, descSetLayout) = CreateBindlessDescriptorSet();
    }
    
    private static unsafe void AddDescriptor(VkImageView view)
    {
        var target = GetValidTexSlot();
        var image = new VkDescriptorImageInfo()
        {
            sampler = default,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            imageView = view,
        };
        var writeSet = new VkWriteDescriptorSet()
        {
            descriptorCount = 1,
            descriptorType = VkDescriptorType.SampledImage,
            dstSet = descriptorset,
            dstBinding = 1,
            dstArrayElement = target,
            pImageInfo = &image,
        };
        vkUpdateDescriptorSets(API.device,writeSet);
    }

    public static EngineImage CreateImage(string path)
    {
        var texture = new EngineImage(path);
        textures.Add(path, texture);
        return texture;
    }

    public static void LoadImage(EngineImage image)
    {
        image.hostImage = Image.Load<Rgba32>(image.path);
    }

    public static unsafe void UploadImage(VkCommandBuffer commandBuffer, EngineImage image,out Action CleanUp)
    {
        image.width = (uint) image.hostImage!.Width;
        image.height = (uint) image.hostImage.Height;
        image.hasMips = true;
        image.mipCount = image.hasMips ? API.MipCount(image.width, image.height) : 1;
        image.imageFormat = VkFormat.R8G8B8A8Srgb;
        image.tiling = VkImageTiling.Optimal;
        image.usage = VkImageUsageFlags.TransferSrc | VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled;
        image.memProperties = VkMemoryPropertyFlags.DeviceLocal;
        image.flags = VkImageCreateFlags.None;
        
        API.AllocateDeviceResourcesForImage(image);
        
        MarkObject(image.deviceImage, "Bindless Texture"u8);
        MarkObject(image.memory, "Bindless Texture Memory"u8);
        API.CreateBuffer(
            image.hostImage.SizeInBytes(),
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible|VkMemoryPropertyFlags.HostCoherent,
            out var stagingBuffer,
            out var mem);
        CleanUp = () =>
        {
            vkDestroyBuffer(API.device, stagingBuffer);
            vkFreeMemory(API.device, mem);
        };
        void* a;
        vkMapMemory(API.device, mem, 0, VK_WHOLE_SIZE, VkMemoryMapFlags.None, &a);
        image.hostImage.CopyPixelDataTo(new Span<byte>(a,(int) image.hostImage.SizeInBytes()));
        
        API.TransitionImageLayout(commandBuffer,image, VkImageLayout.TransferDstOptimal, 0, image.mipCount);
        API.CopyBufferToImage(commandBuffer, stagingBuffer, image.deviceImage, image.width, image.height, 0);
        if (image.hasMips)
        {
            GenerateMipChain(commandBuffer, image, VkImageLayout.TransferDstOptimal, image.imageFormat, image.mipCount, VkImageAspectFlags.Color, VkImageLayout.ShaderReadOnlyOptimal);
        }
        else
        {
            API.TransitionImageLayout(commandBuffer, image,VkImageLayout.ShaderReadOnlyOptimal,0, image.mipCount);
        }


        // CrateView(image);
    }


    public static unsafe void GenerateMipChain(VkCommandBuffer commandBuffer, EngineImage image,
        VkImageLayout initialLayout, VkFormat imageFormat, uint mipCount, VkImageAspectFlags aspect,
        VkImageLayout finalLayout)
    {
        if(initialLayout!=VkImageLayout.TransferSrcOptimal) API.TransitionImageLayoutRaw(commandBuffer, image.deviceImage, initialLayout, VkImageLayout.TransferSrcOptimal, imageFormat, 0, 1);
            
        for (uint i = 0; i < mipCount-1; i++)
        {
            VkImageBlit regions = new()
            {
                srcSubresource = new()
                {
                    aspectMask = aspect,
                    mipLevel = i,
                    baseArrayLayer = 0,
                    layerCount = 1,
                },
                dstSubresource = new()
                {
                    aspectMask = aspect,
                    mipLevel = i + 1,
                    baseArrayLayer = 0,
                    layerCount = 1,
                },
            };
            regions.srcOffsets[0].z = 0;
            regions.srcOffsets[1].z = 1;
            regions.dstOffsets[0].z = 0;
            regions.dstOffsets[1].z = 1;
            if(initialLayout!=VkImageLayout.TransferDstOptimal) API.TransitionImageLayoutRaw(commandBuffer, image.deviceImage, 
                VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, imageFormat, i + 1, 1);
            vkCmdBlitImage(commandBuffer, image.deviceImage, VkImageLayout.TransferSrcOptimal, image.deviceImage,
                VkImageLayout.TransferDstOptimal, 1, &regions, VkFilter.Linear);
            API.TransitionImageLayoutRaw(commandBuffer, image.deviceImage, 
                VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, imageFormat, i + 1, 1);
        }
        API.TransitionImageLayout(commandBuffer, image, finalLayout,0, mipCount);
    }

    public static unsafe uint Bind(EngineImage image)
    {
        VkDescriptorImageInfo imageInfo = new()
        {
            sampler = default,
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            imageView = image.view
        };
        VkWriteDescriptorSet write = new()
        {
            descriptorCount = 1,
            descriptorType = VkDescriptorType.SampledImage,
            dstSet = descriptorset,
            dstBinding = 1,
            dstArrayElement = GetValidTexSlot(),
            pImageInfo = &imageInfo
        };
        vkUpdateDescriptorSets(API.device,write);
        return write.dstArrayElement;
    }
    private static uint GetValidTexSlot()
    {
        return 0;
    }

    private static unsafe (VkDescriptorSet descriptorset, VkDescriptorPool descpool, VkDescriptorSetLayout descSetLayout) CreateBindlessDescriptorSet()
    {
        var bindings = stackalloc[]
        {
            new VkDescriptorSetLayoutBinding
            {
                binding = 1,
                descriptorType = VkDescriptorType.SampledImage,
                descriptorCount = 4000,
                pImmutableSamplers = null,
                stageFlags = VkShaderStageFlags.All,
            }
        };
        var bindingFlags = stackalloc[]
        {
            VkDescriptorBindingFlags.UpdateUnusedWhilePending | VkDescriptorBindingFlags.VariableDescriptorCount |
            VkDescriptorBindingFlags.PartiallyBound,
        };
        var bindflagCI = new VkDescriptorSetLayoutBindingFlagsCreateInfo()
        {
            pBindingFlags = bindingFlags,
            bindingCount = 1,
        };
        var imageLayoutCI = new VkDescriptorSetLayoutCreateInfo()
        {
            bindingCount = 1,
            flags = VkDescriptorSetLayoutCreateFlags.None,
            pBindings = &bindings[0],
            pNext = &bindflagCI,
        };
        vkCreateDescriptorSetLayout(API.device, &imageLayoutCI, null, out var imageSetLayout).Expect();
        var poolsizes = stackalloc[]
        {
            new VkDescriptorPoolSize(VkDescriptorType.SampledImage, 10000),
        };
        var descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo()
        {
            pPoolSizes = poolsizes,
            flags = VkDescriptorPoolCreateFlags.UpdateAfterBind,
            maxSets = 1,
            poolSizeCount = 1,
        };
        vkCreateDescriptorPool(API.device, &descriptorPoolCreateInfo, null, out var pool).Expect();
        var descriptorCount = 100;
        var variableAllocInfo = new VkDescriptorSetVariableDescriptorCountAllocateInfo()
        {
            descriptorSetCount = 1,
            pDescriptorCounts = (uint*) &descriptorCount,
        };
        var descSetAllocInfo = new VkDescriptorSetAllocateInfo()
        {
            descriptorPool = pool,
            descriptorSetCount = 1,
            pSetLayouts = &imageSetLayout,
            pNext = &variableAllocInfo,
        };
        VkDescriptorSet descriptorset;
        vkAllocateDescriptorSets(API.device, &descSetAllocInfo, &descriptorset).Expect();
        return (descriptorset,pool,imageSetLayout);
    }   
}
