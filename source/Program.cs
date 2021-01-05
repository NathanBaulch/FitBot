using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FitBot.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FitBot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Trace.TraceError(e.ExceptionObject.ToString());

            DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => { services.AddHostedService<Worker>(); })
                .Build();

            Trace.Listeners.Add(new ColoredConsoleTraceListener {TraceOutputOptions = TraceOptions.DateTime});
            Trace.Listeners.Add(new ConsoleBeepTraceListener {Filter = new EventTypeFilter(SourceLevels.Warning)});

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Trace.Listeners.Add(new EventLogTraceListener("FitBot") {Filter = new EventTypeFilter(SourceLevels.Warning)});
            }

            var cfg = host.Services.GetService<IConfiguration>();
            var emailTracer = cfg != null ? cfg.GetSection("Smtp").Get<EmailTraceListener>() : new EmailTraceListener();
            emailTracer.Filter = new EventTypeFilter(SourceLevels.Error);
            Trace.Listeners.Add(emailTracer);

            host.Run();
        }
    }
}