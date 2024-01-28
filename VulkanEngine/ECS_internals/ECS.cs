using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VulkanEngine.ECS_internals;


public static class ECS
{
    static Dictionary<Type,Icompstore> dataToSystem = new();
    static Dictionary<Type,Type> refToData = new();
    public static void RegisterSystem<T>(ComponentStorage<T> storage, Type? frontEnd = null) where T : unmanaged, Idata
    {
        dataToSystem.Add(typeof(T),storage);
        if (frontEnd!=null)
            refToData.Add(frontEnd,typeof(T));
    }


    public  struct ECSQuery<T1> where T1:unmanaged
    {
        internal int t1index;
        public int t1count;
        public Memory<int> EntityList,EntityIndices;
    }
    public static unsafe ECSQuery<T1> MakeQuery<T1>(T1* _=null) where T1 : unmanaged, Idata
    {
        var t = (ComponentStorage<T1>)dataToSystem[typeof(T1)];
        return new ECSQuery<T1>() {t1index = 1, t1count = t.used, EntityList = t.EntityList, EntityIndices = t.EntityIndices};
    }
    public static ECSQuery<T1> MakeQuery<T1>(T1 _=default) where T1 : unmanaged, Iinterface
    {
        var t = dataToSystem[typeof(T1)];
        return new ECSQuery<T1>() {t1index = 1, t1count = t.usedProp, EntityList = t.EntityIndicesProp, EntityIndices = t.EntityIndicesProp};
    }
    public static unsafe bool HasResults<T1>(this ref ECSQuery<T1> query,out int globalID,out T1* t1) where T1 :unmanaged,Idata
    {
        if (query.t1index>=query.t1count)
        {
            globalID = default;
            t1 = default;
            return false;
        }
        globalID = query.EntityList.Span[query.t1index];
        
        //start Idata segment
        var t = (ComponentStorage<T1>)dataToSystem[typeof(T1)];
        fixed(T1*_t1=&t.ComponentList.Span[query.t1index])
            t1=_t1;
        //end Idata segment
        
        query.t1index++;
        return true;
    }
    public static unsafe bool HasResults<T1>(this ref ECSQuery<T1> query,out int globalID,out T1 t1) where T1 :unmanaged,Iinterface
    {
        if (query.t1index>=query.EntityList.Length)
        {
            globalID = default;
            t1 = default;
            return false;
        }
        globalID = query.EntityList.Span[query.t1index];
        
        //start Iinterface segmen
        t1=Unsafe.BitCast<int,T1>(query.t1index);
        //end Iinterface segment
        
        query.t1index++;
        return true;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct ECSQuery<T1,T2>
    {
        public int mainindex;
        public int maincount;
        public int mainTypeIndex;
        public Memory<int> EntityListPacked;
        public Memory<int> EntityIndicesSparse1;
    }

    public static unsafe ECSQuery<T1,T2> MakeQuery<T1,T2>(T1* _=null,T2* __=null) where T1 : unmanaged, Idata where T2 : unmanaged, Idata
    {
        var t1 = (ComponentStorage<T1>)dataToSystem[typeof(T1)];
        var t2 = (ComponentStorage<T2>)dataToSystem[typeof(T2)];

        var mainTypeIndex = t1.used<t2.used?1:2;
        var minstorage=mainTypeIndex==1?(Icompstore)t1:t2;
        var otherstorage=mainTypeIndex==1?(Icompstore)t2:t1;
        return new ECSQuery<T1,T2>() {mainindex = 1, maincount = minstorage.usedProp, mainTypeIndex = mainTypeIndex, EntityListPacked = minstorage.EntityListProp, EntityIndicesSparse1 = otherstorage.EntityIndicesProp};
    }
    public static unsafe ECSQuery<T1,T2> MakeQuery<T1,T2>(T1 _=default,T2* __=default) where T1 : unmanaged, Iinterface where T2 : unmanaged, Idata
    {
        var t1 = dataToSystem[typeof(T1)];
        var t2 = (ComponentStorage<T2>)dataToSystem[typeof(T2)];

        var mainTypeIndex = t1.usedProp<t2.used?1:2;
        var minstorage=mainTypeIndex==1?(Icompstore)t1:t2;
        var otherstorage=mainTypeIndex==1?(Icompstore)t2:t1;
        return new ECSQuery<T1,T2>() {mainindex = 1, maincount = minstorage.usedProp, mainTypeIndex = mainTypeIndex, EntityListPacked = minstorage.EntityListProp, EntityIndicesSparse1 = otherstorage.EntityIndicesProp};
    }
    public static ECSQuery<T1,T2> MakeQuery<T1,T2>(T1 _=default,T2 __=default) where T1 : unmanaged, Iinterface where T2 : unmanaged, Iinterface
    {
        var t1 = dataToSystem[typeof(T1)];
        var t2 = dataToSystem[typeof(T2)];

        var mainTypeIndex = t1.usedProp<t2.usedProp?1:2;
        var minstorage=mainTypeIndex==1?(Icompstore)t1:t2;
        var otherstorage=mainTypeIndex==1?(Icompstore)t2:t1;
        return new ECSQuery<T1,T2>() {mainindex = 1, maincount = minstorage.usedProp, mainTypeIndex = mainTypeIndex, EntityListPacked = minstorage.EntityListProp, EntityIndicesSparse1 = otherstorage.EntityIndicesProp};
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TryFindSolutionGivenFirstAsSearch(ref int mainIndex, ref int maxCount, ref Memory<int> EntityListPacked, ref Memory<int> EntityIndicesSparse1)
    {
        while (mainIndex < maxCount)
        {
            var Gid = EntityListPacked.Span[mainIndex];
            var L1 = EntityIndicesSparse1.Span[Gid];
            if (L1 != 0)
            {
                return Gid;
            }

            mainIndex++;
        }

        return 0;
    }

    public static unsafe bool HasResults<T1,T2>(ref ECSQuery<T1,T2> query,out int globalID,out T1* t1,out T2* t2) where T1 :unmanaged,Idata where T2 :unmanaged,Idata
    {
        var Gid=TryFindSolutionGivenFirstAsSearch(ref query.mainindex,ref query.maincount,ref query.EntityListPacked,ref query.EntityIndicesSparse1);
        if (Gid==0)
        {
            globalID = default;
            t1 = default;
            t2 = default;
            return false;
        }
        globalID = Gid;
        var ts1 = (ComponentStorage<T1>)dataToSystem[typeof(T1)];
        fixed(T1*_t1=&ts1.ComponentList.Span[query.mainindex])
            t1=_t1;
        var ts2 = (ComponentStorage<T2>)dataToSystem[typeof(T2)];
        fixed(T2*_t2=&ts2.ComponentList.Span[query.mainindex])
            t2=_t2;
        return true;
    }
    public static unsafe bool HasResults<T1,T2>(ref ECSQuery<T1,T2> query,out int GlobalID,out T1 t1,out T2* t2) where T1 :unmanaged,Iinterface where T2 :unmanaged,Idata
    {
        var Gid=TryFindSolutionGivenFirstAsSearch(ref query.mainindex,ref query.maincount,ref query.EntityListPacked,ref query.EntityIndicesSparse1);
        if (Gid==0)
        {
            GlobalID = default;
            t1 = default;
            t2 = default;
            return false;
        }
        GlobalID = Gid;
        var ts1 = dataToSystem[typeof(T1)];
        t1=Unsafe.BitCast<int,T1>(query.mainindex);
        var ts2 = (ComponentStorage<T2>)dataToSystem[typeof(T2)];
        fixed(T2*_t2=&ts2.ComponentList.Span[query.mainindex])
            t2=_t2;
        return true;
    }
    public static unsafe bool HasResults<T1,T2>(ref ECSQuery<T1,T2> query,out int GlobalID,out T1 t1,out T2 t2) where T1 :unmanaged,Iinterface where T2 :unmanaged,Iinterface
    {
        var Gid=TryFindSolutionGivenFirstAsSearch(ref query.mainindex,ref query.maincount,ref query.EntityListPacked,ref query.EntityIndicesSparse1);
        if (Gid==0)
        {
            GlobalID = default;
            t1 = default;
            t2 = default;
            return false;
        }
        GlobalID = Gid;
        var ts1 = dataToSystem[typeof(T1)];
        t1=Unsafe.BitCast<int,T1>(query.mainindex);
        var ts2 = dataToSystem[typeof(T2)];
        t2=Unsafe.BitCast<int,T2>(query.mainindex);
        return true;
    }

}
public interface Iinterface{}
public interface Idata{}
public interface Iresult{}