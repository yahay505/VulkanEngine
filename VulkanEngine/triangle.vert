#version 460

layout(binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

layout(location = 0) in vec3 inPosition;//per vertex data
layout(location = 1) in vec3 inColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragColor;//per fragment data
layout(location = 1) out vec2 fragTexCoord;

void main() {
    //move x direction by gl_Index*2 model based on gl_InstanceIndex
    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition + vec3(gl_InstanceIndex*2, 0.0, 0.0), 1.0);
//    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragColor = inColor;
    fragTexCoord = inTexCoord;
}
