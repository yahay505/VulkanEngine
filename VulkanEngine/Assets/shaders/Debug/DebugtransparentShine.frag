#version 450

layout(location = 0) in vec2 UV;
layout(location = 0) out vec4 outColor;

void main() {
    outColor = vec4(
    (vec2(1.,1.)
    /
    ((UV*UV).length())
    ),
    0.,
    0.3);
}
