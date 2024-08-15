#version 450
#include "common.glsl" 


layout(set = 0, binding = 0) uniform UniformBufferObject {
    //    mat4 model;
    //    mat4 view;
    //    mat4 proj;
    mat4 viewproj;
} ubo;
layout(set = 0, binding = 2) buffer Compute_OutputBuffer {
    ComputeOutputConfig config;
    ComputeDrawOutput[] data;
} outputData;


layout(location = 0) in vec3 inPosition;//per vertex data
layout(location = 1) in vec3 inVertColor;
layout(location = 2) in vec2 inTexCoord;

layout(location = 0) out vec3 fragVertColor;//per fragment data
layout(location = 1) out vec2 fragTexCoord;
layout(location = 2) out flat float testID;

void main() {
    //move x direction by gl_Index*2 model based on gl_InstanceIndex
    gl_Position = (ubo.viewproj * outputData.data[gl_InstanceIndex].model)* vec4(inPosition, 1.) ;
    //gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragVertColor = inVertColor;
    fragTexCoord = inTexCoord;
    testID = 0.3f;
}
