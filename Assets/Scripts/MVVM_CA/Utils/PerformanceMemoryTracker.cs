using System;
using System.Diagnostics;

public static class PerformanceMemoryTracker
{
    private static long peakPrivateMemoryBytes = 0;

    public static void Sample()
    {
        long current = Process.GetCurrentProcess().PrivateMemorySize64;
        if (current > peakPrivateMemoryBytes)
            peakPrivateMemoryBytes = current;
    }

    public static double PeakPrivateMemoryMB()
    {
        return peakPrivateMemoryBytes / (1024.0 * 1024.0);
    }

    public static void Reset()
    {
        peakPrivateMemoryBytes = 0;
    }
}