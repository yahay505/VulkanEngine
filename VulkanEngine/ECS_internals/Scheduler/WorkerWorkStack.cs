using System.Threading.Channels;

namespace VulkanEngine.ECS_internals;


public class WorkerWorkStack
{
    // make lockfree
    Channel<RuntimeScheduleItemPtr> channel = Channel.CreateUnbounded<RuntimeScheduleItemPtr>();
    // public int tail; // workers side 
    // public int[] ringBuffer = new int[15];
    // public int head; // scheduler side 
    
    public WorkerWorkStack()
    {
        // tail = 0;
        // head = 0;
        // for (int i = 0; i < ringBuffer.Length; i++)
        // {
        //     ringBuffer[i] = -1;
        // }
    }
    
    public bool WorkerHasAny()
    {
        return channel.Reader.TryPeek(out _);
    }
    public bool WorkerTryPop(out RuntimeScheduleItemPtr job)
    {
        return channel.Reader.TryRead(out job);
    }
    public void WorkerPush(RuntimeScheduleItemPtr job)
    {
        unsafe
        {
            job.Item->Function();
            //todo actual implementation
        }
    }

    public bool Steal()
    {
        return false;
    }
    public void OtherAdd(Span<RuntimeScheduleItemPtr> ints)
    {

        foreach (var data in ints)
        {
            channel.Writer.TryWrite(data);
        }
    }
}