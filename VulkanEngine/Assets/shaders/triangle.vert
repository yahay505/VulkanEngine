#version 460

layout(binding = 0) uniform UniformBufferObject {
//    mat4 model;
//    mat4 view;
//    mat4 proj;
    mat4 viewproj;
} ubo;
struct VkDrawIndexedIndirectCommand {
    uint    indexCount;
    uint    instanceCount;
    uint    firstIndex;
    int     vertexOffset;
    uint    firstInstance;
};
struct OutputConfig{
    uint objectCount;
    uint[15] padding;
};
struct Compute_Output {
    VkDrawIndexedIndirectCommand command;
    uint materialID;
    mat4 model;
};
layout(set = 0, binding = 2) buffer Compute_OutputBuffer {
    OutputConfig config;
    Compute_Output[] data;
} outputData;


layout(location = 0) in vec3 inPosition;//per vertex data
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;//per fragment data
layout(location = 1) out vec2 fragTexCoord;

void main() {
    //move x direction by gl_Index*2 model based on gl_InstanceIndex
    gl_Position = ubo.viewproj * outputData.data[gl_InstanceIndex].model * vec4(inPosition, 1.0) ;
//    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragColor = inColor;
    fragTexCoord = inTexCoord;
}
