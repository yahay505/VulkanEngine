using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public class ExecutionGroupChain
{
    // as in dependency chain
    
    //List holding a chain composed of stages with mutually exclusive execution groups
    public List<List<ExecutionUnit>> Groups=new();
}

public class ExecutionUnit
{
    public string Name;
    public List<ExecutionUnit> backlinks = new();
    public List<ExecutionUnit> DependsOn = new();
    public ECSResource[] Reads = Array.Empty<ECSResource>();
    public ECSResource[] Writes = Array.Empty<ECSResource>();
    public Delegate Function = null!;
    public int color = 0;
    public int groupID = 0;
    public int depth = 0;
}
public class ExecutionUnitBuilder
{
    ExecutionUnit unit = new();
    public ExecutionUnitBuilder(Delegate function)
    {
        unit.Function = function;
    }
    public ExecutionUnitBuilder Named(string name)
    {
        unit.Name = name;
        return this;
    }
    // public ExecutionUnitBuilder RunsBefore(params ExecutionUnit[] units)
    // {
    //     unit.RunBefore = units;
    //     return this;
    // }
    public ExecutionUnitBuilder RunsAfter(params ExecutionUnit[] units)
    {
        unit.DependsOn??=new();
        unit.DependsOn.AddRange(units);
        foreach (var sas in units)
        {
            sas.backlinks.Add(unit);
        }
        return this;
    }
    public ExecutionUnitBuilder Reads(params ECSResource[] resources)
    {
        if (resources.Any(r=>r==null))
        {
            throw new Exception("Cannot read null resource");
        }
        unit.Reads = unit.Reads.Concat(resources).ToArray();
        return this;
    }
    public ExecutionUnitBuilder Writes(params ECSResource[] resources)
    {
        if (resources.Any(r=>r==null))
        {
            throw new Exception("Cannot write null resource");
        }
        unit.Writes = unit.Writes.Concat(resources).ToArray();
        return this;
    }
    
    
    
    public ExecutionUnit Build()
    {
        return unit;
    }
}
