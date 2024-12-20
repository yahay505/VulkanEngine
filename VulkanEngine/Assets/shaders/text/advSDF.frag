#version 450

layout(location = 0) in vec2 UV;
layout(location = 0) out vec4 outColor;

float medianThree(float a, float b, float c);
float screenPxRange();

void shade(){
    vec4 s = texture(Sampler2D(_sampler,Tex),UV);
    float median = medianThree(s.r,s.g,s.b);
    float screenPxDistance = screenPxRange()*(median - 0.5);
    float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);
    outColor = mix(vec4(0,0,0,0), vec4(1,1,1,1), opacity);
}
float medianThree(float a, float b, float c) {
    if ((a > b) ^ (a > c))
    return a;
    else if ((b < a) ^ (b < c))
    return b;
    else
    return c;
}
float screenPxRange() {
    vec2 unitRange = vec2(5)/vec2(textureSize(msdf, 0));
    vec2 screenTexSize = vec2(1.0)/fwidth(texCoord);
    return max(0.5*dot(unitRange, screenTexSize), 1.0);
}