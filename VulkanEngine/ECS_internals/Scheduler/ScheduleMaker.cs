namespace VulkanEngine.ECS_internals;

public static class ScheduleMaker
{
    private static Dictionary<string,List<ExecutionUnit>> _units = new();
    public static void RegisterToTarget(ExecutionUnit unit, string target)
    {
        if (_units.ContainsKey(target))
        {
            _units[target].Add(unit);
        }
        else
        {
            _units.Add(target,new List<ExecutionUnit>{unit});
        }
    }

    public static void Build()
    {
        foreach (var (target,units) in _units)
        {
            // color the graph
            foreach (var unit in units)
            {
                unit.color = 0;
            }
// seperate unconnected graphs
            var color = 1;
            foreach (var unit in units)
            {
                var success=colorTree(unit,color);
                if (success)
                {
                    color++;
                }
            }
            
        }
    }

    private static bool colorTree(ExecutionUnit units, int color)
    {
        if (units.color!=0)
        {
            return false;
        }
        units.color = color;
        foreach (var unit in units.RunAfter)
        {
                colorTree(unit, color);
        }
        return true;
    }

}