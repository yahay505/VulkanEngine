namespace VulkanEngine.Renderer;

public class Mesh:IDisposable
{
    private void ReleaseUnmanagedResources()
    {
        // TODO release unmanaged resources here
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~Mesh()
    {
        ReleaseUnmanagedResources();
    }
}