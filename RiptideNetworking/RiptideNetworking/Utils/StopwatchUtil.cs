using System.Diagnostics;

namespace Riptide.Utils
{
    internal static class StopwatchUtil
    {
        // KK91: Framework 3.5 lacks the `Restart` function in stopwatch, so I reimplemented it here
        internal static void Restart(this Stopwatch watch)
        {
            watch.Stop();
            watch.Reset();
            watch.Start();
        }
    }
}
