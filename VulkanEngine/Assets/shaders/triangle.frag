#version 450

layout(location = 0) in vec3 fragColor;
layout(location = 1) in vec2 fragTexCoord;
layout(location = 2) in flat float testID;


layout(location = 0) out vec4 outColor;

layout(binding = 1) uniform sampler2D texSampler;
layout(set = 0, binding = 2) buffer Compute_OutputBuffer {
    ComputeOutputConfig config;
    ComputeDrawOutput[] data;
} computeData;
void main() {
    outColor = texture(texSampler, fragTexCoord)*vec4(fragColor, 1.0)*vec4(testID,1,1,1);
//    outColor = vec4(fragTexCoord,0, 1.0);
}

