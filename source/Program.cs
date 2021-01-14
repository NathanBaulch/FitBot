using FitBot.Achievements;
using FitBot.Logging;
using FitBot.Services;
using Microsoft.Extensions.Configuration;
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
                    .AddSingleton(context.Configuration.GetSection("Database").Get<DatabaseOptions>())
                    .AddSingleton(context.Configuration.GetSection("Fitocracy").Get<FitocracyOptions>())
                    .AddSingleton<IAchievementPushService, AchievementPushService>()
                    .AddSingleton<IAchievementService, AchievementService>()
                    .AddSingleton<IActivityGroupingService, ActivityGroupingService>()
                    .AddSingleton<IDatabaseService, DatabaseService>()
                    .AddSingleton<IFitocracyService, FitocracyService>()
                    .AddSingleton<IScrapingService, ScrapingService>()
                    .AddSingleton<IUserPullService, UserPullService>()
                    .AddSingleton<IWebRequestService, WebRequestService>()
                    .AddSingleton<IWorkoutPullService, WorkoutPullService>()
                    .Scan(scan => scan.FromEntryAssembly().AddClasses(filter => filter.AssignableTo<IAchievementProvider>()).As<IAchievementProvider>())
                    .Decorate<IWebRequestService, ThrottledWebRequestDecorator>()
                    .AddHostedService<Worker>())
                .ConfigureLogging(builder => builder.AddEmailLogger())
                .Build()
                .Run();
    }
}