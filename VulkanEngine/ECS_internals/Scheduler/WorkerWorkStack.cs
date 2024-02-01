using System.Threading.Channels;

namespace VulkanEngine.ECS_internals;


public class WorkerWorkStack
{
    //todo implement  https://sci-hub.se/10.1145/1073970.1073974
    

    
    // make lockfree
    Channel<RuntimeScheduleItem> channel = Channel.CreateUnbounded<RuntimeScheduleItem>();

    // public int tail; // workers side 
    // public int[] ringBuffer = new int[15];
    // public int head; // scheduler side 
    // public int foreignState = 0;

    
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
    public bool WorkerTryPop(out RuntimeScheduleItem job)
    {
        return channel.Reader.TryRead(out job);
    }
    public void WorkerPush(RuntimeScheduleItem job)
    {
        
    }

    public bool Steal()
    {
        return false;
    }
    public void OtherAdd(Span<RuntimeScheduleItem> ints)
    {
        foreach (var data in ints)
        {
            channel.Writer.TryWrite(data);
        }
    }
}