using System;
using System.Diagnostics;

namespace FitBot.Diagnostics
{
    public class ConsoleBeepTraceListener : TraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message) =>
            TraceEvent(eventCache, source, eventType, id, message, Array.Empty<object>());

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }

            if (Environment.UserInteractive)
            {
                Console.Beep();
            }
        }

        public override void Write(string message)
        {
        }

        public override void WriteLine(string message)
        {
        }
    }
}