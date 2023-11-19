namespace VulkanEngine.Renderer.Internal;

public class Mesh_internal:IDisposable
{
    public string name;
    public int indexCount;
    public int vertexCount;
    public ulong indexBufferOffset;
    public ulong vertexBufferOffset;
    public uint[] indexBuffer;
    public Vertex[] vertexBuffer;
    
    public Mesh_internal()
    {
        
    }

    
    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Mesh_internal()
    {
        ReleaseUnmanagedResources();
    }

}