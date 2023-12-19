namespace VulkanEngine.ECS_internals;

public class PagedMemory<T> where T:unmanaged
{
    /*
 EntityIndices

    A sparse array
    Contains integers which are the indices in EntityList.
    The index (not the value) of this sparse array is itself the entity id.

EntityList

    A packed array
    Contains integers - which are the entity ids themselves
    The index doesn't have inherent meaning, other than it must be correct from EntityIndices

ComponentList

    A packed array
    Contains component data (of this pool type)
    It is aligned with EntityList such that the element at EntityList[N] has component data of ComponentList[N]
     */
    public Memory<int> EntityIndices;//global ID to internal ID
    public Memory<int> EntityList;//internal ID to global ID
    public Memory<T> ComponentList;//component data
    public int capacity, used;
    public Span<T> Allocate(ReadOnlySpan<int> globalIDs,out int startIndex)
    {
        var count = globalIDs.Length;
        startIndex = used;
        if (used+count>capacity)
        {
            capacity = Math.Max(capacity * 2, used + count);
            var newEntityIndices = new Memory<int>(new int[capacity]);
            var newEntityList = new Memory<int>(new int[capacity]);
            var newComponentList = new Memory<T>(new T[capacity]);
            EntityIndices.CopyTo(newEntityIndices);
            EntityList.CopyTo(newEntityList);
            ComponentList.CopyTo(newComponentList);
            EntityIndices = newEntityIndices;   
            EntityList = newEntityList;
            ComponentList = newComponentList;
        }
        for (int i = 0; i < count; i++)
        {
            var next_local_ID = i + used;
            EntityIndices.Span[globalIDs[i]] = next_local_ID;
            EntityList.Span[next_local_ID] = globalIDs[i];
        }
        
        
        var ret = ComponentList.Slice(used,count);
        used += count;
        return ret.Span;
    }

    public void Delete_internal(int internalID)
    {
        ComponentList.Span[internalID] = ComponentList.Span[used-1];
        EntityList.Span[internalID] = EntityList.Span[used-1];
        EntityIndices.Span[EntityList.Span[internalID]] = internalID;
        used--;
        ComponentList.Span[used] = default;
        EntityList.Span[used] = default;
        EntityIndices.Span[EntityList.Span[used]] = default;
    }
    
    public void Delete_global(int globalID)
    {
        Delete_internal(EntityIndices.Span[globalID]);
    }
    
}
