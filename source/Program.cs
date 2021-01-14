using FitBot.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FitBot
{
    public static class Program
    {
        private static void Main(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) => services
                    .Configure<EmailLoggerOptions>(context.Configuration.GetSection("Email"))
                    .AddHostedService<Worker>())
                .ConfigureLogging(builder => builder.AddEmailLogger())
                .Build()
                .Run();
    }
}