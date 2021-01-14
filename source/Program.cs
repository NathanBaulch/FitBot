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
                .ConfigureServices((context, services) =>
                {
                    services.Configure<EmailLoggerOptions>(context.Configuration.GetSection("Email"));

                    services.AddSingleton(context.Configuration.GetSection("Database").Get<DatabaseOptions>());
                    services.AddSingleton(context.Configuration.GetSection("Fitocracy").Get<FitocracyOptions>());
                    services.AddSingleton<IAchievementPushService, AchievementPushService>();
                    services.AddSingleton<IAchievementService, AchievementService>();
                    services.AddSingleton<IActivityGroupingService, ActivityGroupingService>();
                    services.AddSingleton<IDatabaseService, DatabaseService>();
                    services.AddSingleton<IFitocracyService, FitocracyService>();
                    services.AddSingleton<IScrapingService, ScrapingService>();
                    services.AddSingleton<IUserPullService, UserPullService>();
                    services.AddSingleton<IWebRequestService, WebRequestService>();
                    services.AddSingleton<IWorkoutPullService, WorkoutPullService>();
                    services.Scan(scan => scan.FromEntryAssembly().AddClasses(filter => filter.AssignableTo<IAchievementProvider>()));
                    services.Decorate<IWebRequestService, ThrottledWebRequestDecorator>();

                    services.AddHostedService<Worker>();
                })
                .ConfigureLogging(builder => builder.AddEmailLogger())
                .Build()
                .Run();
    }
}