namespace VulkanEngine.ECS_internals;

public static class ScheduleMaker
{
    private static Dictionary<string,List<ExecutionUnit>> _units = new();
    private static Dictionary<string,List<ExecutionGroupChain>> _chains = new();
    private static bool built = false;
    public static void RegisterToTarget(ExecutionUnit unit, string target)
    {
        if (built)
        {
            throw new Exception("Cannot register to target after build");
        }
        if (_units.ContainsKey(target))
        {
            _units[target].Add(unit);
        }
        else
        {
            _units.Add(target,new List<ExecutionUnit>{unit});
        }
    }

    public static unsafe void Build()
    {
        if (built)
        {
            throw new Exception("Cannot build twice");
        }
        
        var RuntimeSchedules = new Dictionary<string,RuntimeScheduleState>();

        foreach (var (target,units) in _units)
        {
            //reset colors
            foreach (var unit in units)
            {
                unit.color = 0;
                // normalize reads and writes
                unit.Reads=unit.Reads.Distinct().Except(unit.Writes).ToArray();
            }
            // color the graph 
            var color = 1;
            foreach (var unit in units)
            {
                var success=colorTree(unit,color);
                if (success)
                {
                    color++;
                }
            }
            // seperate unconnected graphs
            var graphs = new List<ExecutionUnit>[color];
            for (int i = 0; i < color; i++)
            {
                graphs[i] = new List<ExecutionUnit>();
            }
            foreach (var unit in units)
            {
                graphs[unit.color-1].Add(unit);
            }

            for (int i = 0; i < color; i++) //check for cycles
            {
                var stack=new Stack<ExecutionUnit>();
                var graph = graphs[i];
                void recurse(ExecutionUnit node)
                {
                    foreach (var nnode in node.DependsOn)
                    {
                        if (stack.Contains(nnode))
                        {
                            throw new("Cyclic dependency detected");
                        }
                        
                        stack.Push(nnode);
                        recurse(nnode);
                        stack.Pop();
                    }
                }
                foreach (var node in graph)
                {
                    recurse(node);
                }
            }
            
            var noBacklinkList =graphs.Select(z=>z.Where(a=>a.backlinks.Count==0)).SelectMany(a=>a).ToList();
            // other_item -dependency-> **item** -wanted by-> Target
            
            foreach (var startNodes in noBacklinkList)
            {
                if (startNodes.depth!=0)
                {
                    continue;
                }
                recurse2(startNodes);
                continue;
                int recurse2(ExecutionUnit node)
                {
                    var sum = 0;
                    foreach (var nnode in node.DependsOn)
                    {
                        sum += recurse2(nnode);
                    }
                    node.depth = sum;
                    return sum+1;
                }
            }//fill depths

            //sort graphs on depth
            var combined = graphs.SelectMany(z=>z.OrderBy(a=>a.depth).ToList()).ToArray();
            
            var result = new List<RuntimeScheduleItem>();
            
            
            {
                var i = 0;
                foreach (var item in combined)
                {
                    result.Add(new RuntimeScheduleItem()
                    {
                        Dependencies = item.DependsOn.Select(a=>combined.ToList().IndexOf(a)).ToArray(),
                        Reads = item.Reads,
                        Writes = item.Writes,
                        IsCompleted = false,
                        IsScheduled = false,
                        Function = (delegate* managed<void>)(void*)item.Function.Method.MethodHandle.GetFunctionPointer()
                    });
                    i++;
                }
            }
            
            
            
            //
            // for (int i = 0; i < color; i++)
            // {
            //     var execchain = new ExecutionGroupChain();
            //     var collected = new HashSet<ExecutionUnit>();
            //     var selector = 0;
            //     while (true)
            //     {
            //         if (collected.Contains(graphs[i][selector]))
            //         {
            //             selector++;
            //             if (selector>=graphs[i].Count)
            //             {
            //                 break;
            //             }
            //             continue;
            //         }
            //         
            //         var parallel_execution_units=new HashSet<ExecutionUnit>(graphs[i]);
            //         recurseRemoveDecendants(graphs[i][selector],parallel_execution_units);
            //         recurseRemoveAncestors(graphs[i][selector],parallel_execution_units);
            //         foreach (var item in parallel_execution_units)
            //         {
            //             collected.Add(item);
            //         }
            //
            //         var rstream= parallel_execution_units.SelectMany(a => a.Reads).Distinct();
            //         var wstream= parallel_execution_units.SelectMany(a => a.Writes).Distinct();
            //         var intersection = rstream.Intersect(wstream).ToArray();
            //         // var binCount= (int)Math.Pow(4,intersection.Length);
            //
            //
            //         execchain.Groups.Add(parallel_execution_units.ToList());
            //     }
            //
            //     if (!_chains.ContainsKey(target))
            //     {
            //         _chains.Add(target, new List<ExecutionGroupChain>());
            //     }
            //
            //     _chains[target].Add(execchain);
            //     continue;
            //     
            //     void recurseRemoveAncestors(ExecutionUnit node,HashSet<ExecutionUnit> set)
            //     {
            //         foreach (var nnode in node.backlinks)
            //         {
            //             if (!set.Contains(nnode)) continue;
            //             set.Remove(nnode);
            //             recurseRemoveAncestors(nnode,set);
            //         }
            //     }
            //     void recurseRemoveDecendants(ExecutionUnit node,HashSet<ExecutionUnit> set)
            //     {
            //         foreach (var nnode in node.DependsOn)
            //         {
            //             if (!set.Contains(nnode)) continue;
            //             set.Remove(nnode);
            //             recurseRemoveDecendants(nnode,set);
            //         }
            //     }
            // }
            //
            
            RuntimeSchedules.Add(target,new RuntimeScheduleState(target,result.ToArray(),false,RuntimeScheduleState.SyncMode.SyncIfRepeated));
            
        }

        ;
        
        Scheduler.RuntimeScheduleStates = RuntimeSchedules;
        
        built = true;
    }
    private static bool colorTree(ExecutionUnit units, int color)
    {
        if (units.color!=0)
        {
            return false;
        }
        units.color = color;
        foreach (var unit in units.DependsOn)
        {
                colorTree(unit, color);
        }
        return true;
    }


}