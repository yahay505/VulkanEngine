﻿using VulkanEngine.ECS_internals.Resources;

namespace VulkanEngine.ECS_internals;

public class ExecutionGraph
{
    List<ExecutionGroupChain> ParallelExecutionGroupChains = new();
}

public class ExecutionGroupChain
{
    // as in dependency chain
    
    //List holding a chain composed of stages with mutually exclusive execution groups
    public List<List<ExecutionGroup>> Groups;
}
public class ExecutionGroup
{
    public ExecutionUnit[] Units;
    public IResource[] Reads;
    public IResource[] Writes;
}
public class ExecutionUnit
{
    public string Name;
    // public ExecutionUnit[] RunBefore = null!;
    public ExecutionUnit[] RunAfter = null!;
    public IResource[] Reads = null!;
    public IResource[] Writes = null!;
    public Delegate Function = null!;
    public int color= 0;
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
        unit.RunAfter = units;
        return this;
    }
    public ExecutionUnitBuilder Reads(params IResource[] resources)
    {
        unit.Reads = resources;
        return this;
    }
    public ExecutionUnitBuilder Writes(params IResource[] resources)
    {
        unit.Writes = resources;
        return this;
    }
    
    
    
    public ExecutionUnit Build()
    {
        return unit;
    }
}


public interface IExecutionStreamThing {}
