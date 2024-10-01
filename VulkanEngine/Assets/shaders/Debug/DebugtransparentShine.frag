#version 450

layout(location = 0) in vec2 UV;
layout(location = 0) out vec4 outColor;
float shape = 2;
void main() {
    float sqdist =pow(10*abs(UV-.5).x,shape)+pow(10*abs(UV-.5).y,shape);
    float light = 1/sqdist;
        outColor = vec4(light,light,light*.2,0);
}
