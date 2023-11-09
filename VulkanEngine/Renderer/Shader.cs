namespace VulkanEngine.Renderer;

public class Shader:IDisposable
{
    public Shader()
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
    ~Shader()
    {
        ReleaseUnmanagedResources();
    }
}