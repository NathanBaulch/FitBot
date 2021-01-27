using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace FitBot.Logging
{
    public class CustomConsoleLoggerFormatter : ConsoleFormatter
    {
        private readonly ConsoleFormatter _inner;

        public CustomConsoleLoggerFormatter(ConsoleFormatter inner)
            : base(inner.Name) => _inner = inner;

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var le = logEntry;
            var ex = le.Exception;
            if (ex != null)
            {
                le = new LogEntry<TState>(le.LogLevel, le.Category, le.EventId, le.State, null, le.Formatter);
            }
            _inner.Write(le, scopeProvider, textWriter);
            if (ex != null)
            {
                textWriter.WriteLine(ex);
            }
        }
    }
}