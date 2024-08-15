#version 450

layout(location = 0) in vec2 inPosition;//per vertex data
layout(location = 1) in vec2 inTexCoord;

layout(location = 0) out vec2 UV;


void main() {
    gl_Position = vec4(inPosition.x,inPosition.y,0.999,1);
    UV = 1.-inTexCoord;
}
