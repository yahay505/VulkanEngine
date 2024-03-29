﻿namespace VulkanEngine.ECS_internals;

public class RuntimeScheduleState
{
    public string TargetName;
    public readonly RuntimeScheduleItem[] Items;
    public SyncMode syncMode;
    public bool SyncAtEnd;
    public int Tail=0;
    public int Head=0;
    public bool IsCompleted;

    public RuntimeScheduleState(string targetName, RuntimeScheduleItem[] items, bool syncAtEnd, SyncMode syncMode)
    {
        TargetName = targetName;
        Items = items;
        SyncAtEnd = syncAtEnd;
        this.syncMode = syncMode;
    }

    public void Reset()
    {
        for (int i = 0; i < Items.Length; i++)
        {
            Volatile.Write(ref Items[i].IsCompleted, false);
            Volatile.Write(ref Items[i].IsScheduled, false);
        }
        Volatile.Write(ref Tail, 0);
        Volatile.Write(ref Head, 0);
        Volatile.Write(ref IsCompleted, false); 
    }

    public enum SyncMode
    {
        NA,
        /// <summary>
        /// BROKEN (NEED TO CANGE BOOL TO INT in finished counter )
        /// </summary>
        DontSync,
        SyncAtEnd,
        SyncIfRepeated,
    }
}