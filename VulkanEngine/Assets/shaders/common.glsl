//#version 450

// 20 bytes
struct VkDrawIndexedIndirectCommand {
    uint    indexCount;
    uint    instanceCount;
    uint    firstIndex;
    int     vertexOffset;
    uint    firstInstance;
};
// 16 bytes
struct MeshInfo{
    uint IBOoffset;
    uint IBOsize;
    int vertexLoadOffset;
    int padding;
};
struct ComputeMeshInput {
    mat4 transform; // 64 bytes
    uint meshID; // 4 bytes
    uint materialID; // 4 bytes
    uint padding[14]; // 56 bytes
};
// 128 bytes
struct ComputeDrawOutput {
    VkDrawIndexedIndirectCommand command; // 20 bytes
    int padding[3]; // 12 bytes
    int MaterialData[8]; // 32 bytes
    mat4 model; // 64 bytes
};
// 64 bytes
struct ComputeInputConfig {
    uint objectCount;
    uint[15] padding;
};
// 64 bytes
struct ComputeOutputConfig {
    uint objectCount;
    uint[15] padding;
};