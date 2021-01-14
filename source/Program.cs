using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using FitBot.Achievements;
using FitBot.Logging;
using FitBot.Services;
using FitBot.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitBot
{
    public static class Program
    {
        private static void Main(string[] args) =>
            BuildCommandLine()
                .Build()
                .Invoke(args);

        private static CommandLineBuilder BuildCommandLine()
        {
            var rehash = new Command("rehash")
                {
                    new Option<bool>("--gentle"),
                    new Option<bool>("--dry-run")
                };
            rehash.Handler = CommandHandler.Create<IHost, bool, bool, CancellationToken>((host, gentle, dryRun, cancel) => host.Services.GetRequiredService<Rehasher>().Run(gentle, dryRun, cancel));
            var root = new RootCommand {rehash};
            root.Handler = CommandHandler.Create(Execute);
            return new CommandLineBuilder(root)
                .UseHost(_ => CreateHostBuilder()
                    .ConfigureServices(services =>
                    {
                        services.Configure<InvocationLifetimeOptions>(options => options.SuppressStatusMessages = true);
                        services.AddSingleton<Rehasher>();
                    })
                )
                .UseDefaults()
                .UseExceptionHandler((ex, _) =>
                {
                    if (ex.GetBaseException() is not OperationCanceledException)
                    {
                        throw ex;
                    }
                });
        }

        private static void Execute() =>
            CreateHostBuilder()
                .UseWindowsService()
                .ConfigureServices((context, services) =>
                {
                    services.Configure<EmailLoggerOptions>(context.Configuration.GetSection("Email"));
                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging(builder => builder.AddEmailLogger())
                .Build()
                .Run();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton(context.Configuration.GetSection("Database").Get<DatabaseOptions>());
                    services.AddSingleton(context.Configuration.GetSection("Fitocracy").Get<FitocracyOptions>());
                    services.AddSingleton<IAchievementPushService, AchievementPushService>();
                    services.AddSingleton<IAchievementService, AchievementService>();
                    services.AddSingleton<IActivityGroupingService, ActivityGroupingService>();
                    services.AddSingleton<IActivityHashService, ActivityHashService>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<IFitocracyService, FitocracyService>();
                    services.AddSingleton<IScrapingService, ScrapingService>();
                    services.AddSingleton<IUserPullService, UserPullService>();
                    services.AddSingleton<IWebRequestService, WebRequestService>();
                    services.AddSingleton<IWorkoutPullService, WorkoutPullService>();
                    services.Scan(scan => scan.FromEntryAssembly().AddClasses(filter => filter.AssignableTo<IAchievementProvider>()));
                    services.Decorate<IWebRequestService, ThrottledWebRequestDecorator>();
                })
                .ConfigureLogging(builder => builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ssK ";
                }));
    }
}