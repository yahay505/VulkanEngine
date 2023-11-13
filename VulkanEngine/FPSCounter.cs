using System.Diagnostics;

namespace VulkanEngine;

public class FPSCounter
{
    private long[] buffer;
    private int head=0, tail=0;

    public FPSCounter(int maxFPS)
    {
        buffer = new long[maxFPS];
    }
    public int AddAndGetFrame()
    {
        buffer[head] = Stopwatch.GetTimestamp();
        var sec = Stopwatch.Frequency;
        while (buffer[head] - buffer[tail] > sec)
            tail = (tail + 1) % buffer.Length;
        head = (head + 1) % buffer.Length;
        
        return (head - tail + buffer.Length) % buffer.Length;
    }
}