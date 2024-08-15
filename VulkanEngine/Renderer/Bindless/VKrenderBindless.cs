using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using static VulkanEngine.Renderer.GPUDEBUG;

namespace VulkanEngine.Renderer;

public static class TextureManager
{
    public static VkDescriptorSet descriptorset;
    private static VkDescriptorPool descpool;
    public static VkDescriptorSetLayout descSetLayout;
    private static Dictionary<string,EngineTexture> textures = new();

    internal static unsafe void InitTextureEngine()
    {
         (descriptorset, descpool, descSetLayout) = CreateBindlessDescriptorSet();
    }
    
    private static unsafe void AddDescriptor(VkImageView view, VkImageLayout layout)
    {
        var target = GetValidTexSlot();
        var image = new VkDescriptorImageInfo()
        {
            sampler = default,
            imageLayout = layout,
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
        vkUpdateDescriptorSets(VKRender.device,writeSet);
    }

    public static EngineTexture CreateImage(string path)
    {
        var texture = new EngineTexture(path);
        textures.Add(path, texture);
        return texture;
    }

    public static void LoadImage(EngineTexture image)
    {
        image.hostImage = Image.Load<Rgba32>(image.path);
    }

    public static unsafe void UploadImage(VkCommandBuffer commandBuffer, EngineTexture image,out Action CleanUp)
    {
        image.width = (uint) image.hostImage!.Width;
        image.height = (uint) image.hostImage.Height;
        image.hasMips = true;
        image.mipCount = image.hasMips ? VKRender.MipCount(image.width, image.height) : 1;
        image.imageFormat = VkFormat.R8G8B8A8Srgb;
        
        VKRender.CreateImage(
            image.width,
            image.height,
            image.imageFormat,
            VkImageTiling.Optimal,
            VkImageUsageFlags.TransferSrc|VkImageUsageFlags.TransferDst|VkImageUsageFlags.Sampled,
            VkMemoryPropertyFlags.DeviceLocal,
            image.hasMips,
            VkImageCreateFlags.None,
            out image.deviceImage,
            out image.memory);
        MarkObject(image.deviceImage, "Bindless Texture"u8);
        MarkObject(image.memory, "Bindless Texture Memory"u8);
        VKRender.CreateBuffer(
            image.hostImage.SizeInBytes(),
            VkBufferUsageFlags.TransferSrc,
            VkMemoryPropertyFlags.HostVisible|VkMemoryPropertyFlags.HostCoherent,
            out var stagingBuffer,
            out var mem);
        CleanUp = () =>
        {
            vkDestroyBuffer(VKRender.device, stagingBuffer);
            vkFreeMemory(VKRender.device, mem);
        };
        void* a;
        vkMapMemory(VKRender.device, mem, 0, VK_WHOLE_SIZE, VkMemoryMapFlags.None, &a);
        image.hostImage.CopyPixelDataTo(new Span<byte>(a,(int) image.hostImage.SizeInBytes()));
        
        VKRender.TransitionImageLayout(commandBuffer,image.deviceImage, image.imageFormat, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal, 0, image.mipCount);
        VKRender.CopyBufferToImage(commandBuffer, stagingBuffer, image.deviceImage, image.width, image.height, 0);
        image.layout = VkImageLayout.ShaderReadOnlyOptimal;
        
        if (image.hasMips)
        {
            GenerateMipChain(commandBuffer, image, VkImageLayout.TransferDstOptimal, image.imageFormat, image.mipCount, VkImageAspectFlags.Color, image.layout);
        }
        else
        {
            VKRender.TransitionImageLayout(commandBuffer, image.deviceImage, image.imageFormat, VkImageLayout.TransferDstOptimal, image.layout,0, image.mipCount);
        }


        CrateView(image);
    }

    public static unsafe void CrateView(EngineTexture image)
    {
        VkImageViewCreateInfo viewCreateInfo = new()
        {
            flags = VkImageViewCreateFlags.None,
            image = image.deviceImage,
            subresourceRange = new()
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = image.mipCount,
                baseArrayLayer = 0,
                layerCount = 1,
            },
            format = image.imageFormat,
            components = VkComponentMapping.Rgba,
            viewType = VkImageViewType.Image2D,
            pNext = null,
        };
        vkCreateImageView(VKRender.device, &viewCreateInfo, null, out image.view);
        MarkObject(image.view, "Bindless Texture View"u8);
    }

    public static unsafe void GenerateMipChain(VkCommandBuffer commandBuffer, EngineTexture image,
        VkImageLayout initialLayout, VkFormat imageFormat, uint mipCount, VkImageAspectFlags aspect,
        VkImageLayout finalLayout)
    {
        if(initialLayout!=VkImageLayout.TransferSrcOptimal) VKRender.TransitionImageLayout(commandBuffer, image.deviceImage, imageFormat, initialLayout, VkImageLayout.TransferSrcOptimal,0, 1);
            
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
            if(initialLayout!=VkImageLayout.TransferDstOptimal) VKRender.TransitionImageLayout(commandBuffer, image.deviceImage, imageFormat,
                VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, i + 1, 1);
            vkCmdBlitImage(commandBuffer, image.deviceImage, VkImageLayout.TransferSrcOptimal, image.deviceImage,
                VkImageLayout.TransferDstOptimal, 1, &regions, VkFilter.Linear);
            VKRender.TransitionImageLayout(commandBuffer, image.deviceImage, imageFormat,
                VkImageLayout.TransferDstOptimal, VkImageLayout.TransferSrcOptimal, i + 1, 1);
        }
        VKRender.TransitionImageLayout(commandBuffer, image.deviceImage, imageFormat, VkImageLayout.TransferSrcOptimal, finalLayout,0, mipCount);
    }

    public static unsafe uint Bind(EngineTexture image)
    {
        VkDescriptorImageInfo imageInfo = new()
        {
            sampler = default,
            imageLayout = image.layout,
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
        vkUpdateDescriptorSets(VKRender.device,write);
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
        vkCreateDescriptorSetLayout(VKRender.device, &imageLayoutCI, null, out var imageSetLayout).Expect();
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
        vkCreateDescriptorPool(VKRender.device, &descriptorPoolCreateInfo, null, out var descpool).Expect();
        var descriptorCount = 100;
        var variableAllocInfo = new VkDescriptorSetVariableDescriptorCountAllocateInfo()
        {
            descriptorSetCount = 1,
            pDescriptorCounts = (uint*) &descriptorCount,
        };
        var descSetAllocInfo = new VkDescriptorSetAllocateInfo()
        {
            descriptorPool = descpool,
            descriptorSetCount = 1,
            pSetLayouts = &imageSetLayout,
            pNext = &variableAllocInfo,
        };
        VkDescriptorSet descriptorset;
        vkAllocateDescriptorSets(VKRender.device, &descSetAllocInfo, &descriptorset).Expect();
        return (descriptorset,descpool,imageSetLayout);
    }   
}

public class EngineTexture(string path)
{
    public string path = path;
    // public GPUResourceStatus status;
    public Image<Rgba32>? hostImage;
    public VkImage deviceImage;
    public VkImageView view;
    public VkDeviceMemory memory;
    public VkImageLayout layout;
    public uint width;
    public uint height;
    public bool hasMips;
    public uint mipCount;
    public VkFormat imageFormat;
}

[Flags]
public enum GPUResourceStatus
{
    Disk=1,
    RAM=2,
    VRAM=4,
}
