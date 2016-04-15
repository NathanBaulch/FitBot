using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace FitBot.Diagnostics
{
    public class ColoredConsoleTraceListener : ConsoleTraceListener
    {
        private readonly Dictionary<TraceEventType, ConsoleColor> _eventColor;

        public ColoredConsoleTraceListener()
        {
            _eventColor = new Dictionary<TraceEventType, ConsoleColor>
                {
                    {TraceEventType.Verbose, ConsoleColor.DarkGray},
                    {TraceEventType.Information, ConsoleColor.Gray},
                    {TraceEventType.Warning, ConsoleColor.Yellow},
                    {TraceEventType.Error, ConsoleColor.DarkRed},
                    {TraceEventType.Critical, ConsoleColor.Red},
                    {TraceEventType.Start, ConsoleColor.DarkCyan},
                    {TraceEventType.Stop, ConsoleColor.DarkCyan}
                };
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, message, new object[0]);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (Filter != null && !Filter.ShouldTrace(eventCache, source, eventType, id, format, args, null, null))
            {
                return;
            }

            var oldColor = Console.ForegroundColor;
            ConsoleColor newColor;
            Console.ForegroundColor = _eventColor.TryGetValue(eventType, out newColor) ? newColor : oldColor;
            try
            {
                WriteLine(string.Format(CultureInfo.InvariantCulture, format, args));
            }
            finally
            {
                Console.ForegroundColor = oldColor;
            }
        }
    }
}