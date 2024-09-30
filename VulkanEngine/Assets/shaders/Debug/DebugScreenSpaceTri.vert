#version 450
//layout(location = 0) in vec2 inPosition;//per vertex data
//layout(location = 1) in vec2 inTexCoord;

layout(location = 0) out vec2 UV;
//layout(location = 0) in vec3 inPosition;//per vertex data
////layout(location = 1) in vec3 inVertColor;
//layout(location = 1) in vec2 inTexCoord;

void main() {
    
    UV =    gl_VertexIndex==0 ? vec2(0.,0.):
                gl_VertexIndex==1 ? vec2(0.,2.):
                                     vec2(2.,0.);
    gl_Position = vec4(UV,0.,1.);
    UV = vec2(1);
}
