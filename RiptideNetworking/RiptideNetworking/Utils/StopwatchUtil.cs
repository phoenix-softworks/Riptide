using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Riptide.Utils
{
    internal static class StopwatchUtil
    {
        // KK91: Framework 3.5 lacks the `Reset` function in stopwatch, so I reimplemented it here
        internal static void Reset(this Stopwatch watch)
        {
            watch.Stop();
            watch.Reset();
            watch.Start();
        }
    }
}
