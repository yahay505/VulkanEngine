namespace VulkanEngine.ECS_internals;
// [ECSScan]
public static class RenderJob
{
    static RenderJob()
    {
        // Scheduler.RegisterRecurrent();
    }
    
    // [ECSJob(typeof(ECSQuery<Transform_ref,MeshData>),Reads=new []{Transform_ref,MeshData},Writes=null,DependsOn=null,WantedBy=null)]
    public static void Render(ECSQuery<Transform_ref,MeshData> query)
    {
        unsafe // unsafe because MeshData doesn't have a proxy struct(so mesh is a MeshData*)
        {
            while (HasResults(ref query,out _,out var transform,out var mesh))
            {
                // code here
            }
        }
    }
}