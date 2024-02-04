namespace VulkanEngine.ECS_internals;
[ECSScan]
public static class RenderJob
{
    static RenderJob()
    {
        // Scheduler.RegisterRecurrent();
    }

    public static void Register()
    {
        var a=new ExecutionUnitBuilder(Render)
            .Named("Render")
            .Reads()
            .Writes()
            .RunsAfter( /*ticksDone*/)
            .Build();
        // Scheduler.RegisterRecurrent(a);
    }
    [ECSJob(nameof(Render)
        // typeof(ECSQuery<Transform_ref,MeshData>),Reads=new []{Transform_ref,MeshData},Writes=null,DependsOn=null,RunBefore=null
        )]
    public static void Render(ref ECSQuery<Transform_ref,Mesh_ref> query)
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