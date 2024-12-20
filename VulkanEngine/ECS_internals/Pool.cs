using System.Runtime.CompilerServices;

namespace VulkanEngine.ECS_internals;

public interface Icompstore
{
    /// <summary>
    /// global ID to internal ID
    /// </summary>
    ref Memory<int> EntityIndicesProp { get; }
    /// <summary>
    /// internal ID to global ID
    /// </summary>
    ref Memory<int> EntityListProp { get; }
    ref int usedProp { get; }
}
public class Pool<T>:Icompstore where T:unmanaged,Idata
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
    public ref Memory<int> EntityIndicesProp => ref EntityIndices;
    public ref Memory<int> EntityListProp => ref EntityList;
    public ref int usedProp => ref this.used;
    /// <summary>
    /// global ID to internal ID
    /// </summary>
    public Memory<int> EntityIndices;
    /// <summary>
    /// internal ID to global ID
    /// </summary>
    public Memory<int> EntityList;
    /// <summary>
    /// internal ID to component data
    /// </summary>
    public Memory<T> ComponentList;
    public int capacity, used;
    public bool tagOnly;
    public Pool(bool tagOnly, int initial_capacity)
    {
        this.tagOnly = tagOnly;
        capacity = Math.Max(1,initial_capacity);
        used = 1;//0 is reserved for invalid
        EntityIndices = new Memory<int>(new int[capacity]);
        EntityList = new Memory<int>(new int[capacity]);
        if (!tagOnly)
            ComponentList = new Memory<T>(new T[capacity]);
    }
    public Pool(Memory<byte> bundle)
    {
        // throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int internalID(int globalID)
    {
        return EntityIndices.Span[globalID];
    }
    public int globalID(int internalID)
    {
        return EntityList.Span[internalID];
    }
    public ref T GetComponentGlobalID(int globalID)
    {
        return ref ComponentList.Span[EntityIndices.Span[globalID]];
    }
    public ref T GetComponentInternalID(int internalID)
    {
        return ref ComponentList.Span[internalID];
    }
    
        
    private Span<T> Allocate(ReadOnlySpan<int> globalIDs,out int startIndex)
    {
        var count = globalIDs.Length;
        startIndex = used;
        if (used+count>=capacity)
        {
            capacity = Math.Max(capacity * 2, used + count);
            var newEntityIndices = new Memory<int>(new int[capacity]);
            EntityIndices.CopyTo(newEntityIndices);
            EntityIndices = newEntityIndices;   
            var newEntityList = new Memory<int>(new int[capacity]);
            EntityList.CopyTo(newEntityList);
            EntityList = newEntityList;
            if (!tagOnly)
            {
                var newComponentList = new Memory<T>(new T[capacity]);
                ComponentList.CopyTo(newComponentList);
                ComponentList = newComponentList;
            }
        }
        for (int i = 0; i < count; i++)
        {
            var next_local_ID = i + used;
            EntityIndices.Span[globalIDs[i]] = next_local_ID;
            EntityList.Span[next_local_ID] = globalIDs[i];
        }
        
        var oldUsed = used; 
        used += count;
        if (!tagOnly)
        {
            var ret = ComponentList.Slice(oldUsed ,count);
            return ret.Span;
        }
        return default;
    }
    public void Delete_internal(int internalID)
    {
        if (!tagOnly)
            ComponentList.Span[internalID] = ComponentList.Span[used-1];
        EntityList.Span[internalID] = EntityList.Span[used-1];
        EntityIndices.Span[EntityList.Span[internalID]] = internalID;
        used--;
        if (!tagOnly)
            ComponentList.Span[used] = default;
        EntityList.Span[used] = default;
        EntityIndices.Span[EntityList.Span[used]] = default;
    }
    public void Delete_global(int globalID)
    {
        Delete_internal(EntityIndices.Span[globalID]);
    }
    public Memory<byte> Bundle()
    {
        throw new NotImplementedException();
    }

    public int AddItemWithGlobalID(int globalID, T data)
    {
        var span = Allocate(stackalloc int[]{globalID},out var internalID);
        span[0] = data;
        return internalID;
    }
}
