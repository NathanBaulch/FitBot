using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using FitBot.Achievements;
using FitBot.Services;
using SimpleInjector;

namespace FitBot
{
    public partial class WinService : ServiceBase
    {
        private Container _container;
        private CancellationTokenSource _cancelSource;

        public void Start()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            _container = new Container();

            _container.RegisterSingleton<IAchievementPushService, AchievementPushService>();
            _container.RegisterSingleton<IAchievementService, AchievementService>();
            _container.RegisterSingleton<IActivityGroupingService, ActivityGroupingService>();
            _container.RegisterSingleton<IDatabaseService, DatabaseService>();
            _container.RegisterSingleton<IFitocracyService, FitocracyService>();
            _container.RegisterSingleton<IScrapingService, ScrapingService>();
            _container.RegisterSingleton<IUserPullService, UserPullService>();
            _container.RegisterSingleton<IWebRequestService, WebRequestService>();
            _container.RegisterSingleton<IWorkoutPullService, WorkoutPullService>();

            _container.Collection.Register<IAchievementProvider>(
                GetType().Assembly.GetTypes()
                         .Where(typeof (IAchievementProvider).IsAssignableFrom)
                         .Where(type => type.IsClass));

            _container.RegisterDecorator<IWebRequestService, ThrottledWebRequestDecorator>(Lifestyle.Singleton);

            _container.Verify();

            _cancelSource = new CancellationTokenSource();
            Run();
        }

        protected override void OnStop()
        {
            _cancelSource.Cancel();
        }

        private void Run()
        {
            var throttler = (ThrottledWebRequestDecorator) _container.GetInstance<IWebRequestService>();
            var errorBackoff = 0;

            while (!_cancelSource.IsCancellationRequested)
            {
                var watch = Stopwatch.StartNew();

                try
                {
                    Execute(_container);
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

                    Thread.Sleep(TimeSpan.FromSeconds((int) Math.Pow(2, errorBackoff)));
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

        private void Execute(Container container)
        {
            var userPull = container.GetInstance<IUserPullService>();
            var workoutPull = container.GetInstance<IWorkoutPullService>();
            var achieveService = container.GetInstance<IAchievementService>();
            var achievementPush = container.GetInstance<IAchievementPushService>();

            foreach (var user in userPull.Pull(_cancelSource.Token))
            {
                var workouts = workoutPull.Pull(user, _cancelSource.Token);
                var achievements = achieveService.Process(user, workouts, _cancelSource.Token);
                achievementPush.Push(achievements, _cancelSource.Token);
            }
        }
    }
}