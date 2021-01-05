using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SimpleInjector;

namespace FitBot
{
    public class Worker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private Container _container;

        public Worker(IConfiguration configuration) => _configuration = configuration;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _container = new Container();

            _container.Register<IAchievementPushService, AchievementPushService>();
            _container.Register<IAchievementService, AchievementService>();
            _container.Register<IActivityGroupingService, ActivityGroupingService>();
            _container.Register(() => _configuration.GetSection("Database").Get<DatabaseOptions>(), Lifestyle.Singleton);
            _container.Register<IDatabaseService, DatabaseService>();
            _container.Register(() => _configuration.GetSection("Fitocracy").Get<FitocracyOptions>(), Lifestyle.Singleton);
            _container.Register<IFitocracyService, FitocracyService>();
            _container.Register<IScrapingService, ScrapingService>();
            _container.Register<IUserPullService, UserPullService>();
            _container.Register<IWebRequestService, WebRequestService>();
            _container.Register<IWorkoutPullService, WorkoutPullService>();

            _container.Collection.Register<IAchievementProvider>(
                GetType().Assembly.GetTypes()
                    .Where(typeof(IAchievementProvider).IsAssignableFrom)
                    .Where(type => type.IsClass));

            _container.RegisterDecorator<IWebRequestService, ThrottledWebRequestDecorator>();

            _container.Verify();

            await Run(stoppingToken);
        }

        private async Task Run(CancellationToken stoppingToken)
        {
            var throttler = (ThrottledWebRequestDecorator) _container.GetInstance<IWebRequestService>();
            var errorBackoff = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                var watch = Stopwatch.StartNew();

                try
                {
                    await Execute(_container, stoppingToken);
                    errorBackoff = 0;
                }
                catch (Exception ex)
                {
                    var aggregateEx = (ex as AggregateException)?.Flatten() ?? new AggregateException(ex);

                    foreach (var inner in aggregateEx.InnerExceptions)
                    {
                        if (inner is OperationCanceledException)
                        {
                            return;
                        }

                        Trace.TraceError(inner.ToString());
                    }

                    await Task.Delay(TimeSpan.FromSeconds((int) Math.Pow(2, errorBackoff)), stoppingToken);
                    errorBackoff++;
                }

                var elapsed = watch.Elapsed;
                Trace.TraceInformation("Processing time: " + elapsed);

                if (errorBackoff == 0)
                {
                    if (elapsed > TimeSpan.FromHours(1))
                    {
                        throttler.ThrottleFactor--;
                    }
                    else
                    {
                        throttler.ThrottleFactor++;
                    }
                }
            }
        }

        private static async Task Execute(Container container, CancellationToken stoppingToken)
        {
            var userPull = container.GetInstance<IUserPullService>();
            var workoutPull = container.GetInstance<IWorkoutPullService>();
            var achieveService = container.GetInstance<IAchievementService>();
            var achievementPush = container.GetInstance<IAchievementPushService>();

            foreach (var user in await userPull.Pull(stoppingToken))
            {
                var workouts = await workoutPull.Pull(user, stoppingToken);
                var achievements = await achieveService.Process(user, workouts, stoppingToken);
                await achievementPush.Push(achievements, stoppingToken);
            }
        }
    }
}