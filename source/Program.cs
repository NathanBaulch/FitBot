using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Logging;
using FitBot.Services;
using FitBot.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitBot
{
    public static class Program
    {
        private static Task Main(string[] args) =>
            BuildCommandLine()
                .Build()
                .InvokeAsync(args);

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
                    .ConfigureServices(services => services
                        .Configure<InvocationLifetimeOptions>(options => options.SuppressStatusMessages = true)
                        .AddSingleton<Rehasher>())
                    .ConfigureLogging(builder => builder.Services.RemoveAll<ILoggerProvider>()))
                .UseDefaults()
                .UseExceptionHandler((ex, _) =>
                {
                    if (ex.GetBaseException() is not OperationCanceledException)
                    {
                        throw ex;
                    }
                });
        }

        private static Task Execute() =>
            CreateHostBuilder()
                .UseWindowsService()
                .ConfigureServices((context, services) => services
                    .Configure<EmailLoggerOptions>(context.Configuration.GetSection("Email"))
                    .AddHostedService<Worker>())
                .ConfigureLogging(builder => builder
                    .AddEmailLogger()
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss ";
                    }))
                .Build()
                .RunAsync();

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => services
                    .AddSingleton(context.Configuration.GetSection("Database").Get<DatabaseOptions>())
                    .AddSingleton(context.Configuration.GetSection("Fitocracy").Get<FitocracyOptions>())
                    .AddSingleton<IAchievementPushService, AchievementPushService>()
                    .AddSingleton<IAchievementService, AchievementService>()
                    .AddSingleton<IActivityGroupingService, ActivityGroupingService>()
                    .AddSingleton<IActivityHashService, ActivityHashService>()
                    .AddSingleton<IDatabaseService, DatabaseService>()
                    .AddSingleton<IFitocracyService, FitocracyService>()
                    .AddSingleton<IScrapingService, ScrapingService>()
                    .AddSingleton<IUserPullService, UserPullService>()
                    .AddSingleton<IWebRequestService, WebRequestService>()
                    .AddSingleton<IWorkoutPullService, WorkoutPullService>()
                    .Scan(scan => scan.FromEntryAssembly().AddClasses(filter => filter.AssignableTo<IAchievementProvider>()).As<IAchievementProvider>())
                    .Decorate<IWebRequestService, ThrottledWebRequestDecorator>());
    }
}