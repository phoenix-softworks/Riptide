namespace System.Diagnostics
{
    internal static class StopwatchUtil
    {
        // KK91: Framework 3.5 lacks the `Reset` function in stopwatch, so I reimplemented it here
        internal static void Restart(this Stopwatch watch)
        {
            watch.Stop();
            watch.Reset();
            watch.Start();
        }
    }
}
