using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Services;
using SimpleInjector;

namespace FitBot
{
    public partial class WinService : ServiceBase
    {
        private Container _container;
        private CancellationTokenSource _cancelSource;
        private Task _task;

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

            _container.RegisterCollection<IAchievementProvider>(
                GetType().Assembly.GetTypes()
                         .Where(typeof (IAchievementProvider).IsAssignableFrom)
                         .Where(type => type.IsClass));

            _container.RegisterDecorator<IWebRequestService, ThrottledWebRequestDecorator>(Lifestyle.Singleton);

            _container.Verify();

            _cancelSource = new CancellationTokenSource();
            _task = Task.Factory.StartNew(Run, _cancelSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        protected override void OnStop()
        {
            _cancelSource.Cancel();
            try
            {
                _task.Wait();
            }
            catch
            {
            }
        }

        private void Run()
        {
            var throttler = (ThrottledWebRequestDecorator) _container.GetInstance<IWebRequestService>();

            while (!_cancelSource.IsCancellationRequested)
            {
                var watch = Stopwatch.StartNew();

                try
                {
                    Execute(_container).Wait();
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
                }

                var elapsed = watch.Elapsed;
                Debug.WriteLine("Processing time: " + elapsed);
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

        private async Task Execute(Container container)
        {
            var userPull = container.GetInstance<IUserPullService>();
            var workoutPull = container.GetInstance<IWorkoutPullService>();
            var achieveService = container.GetInstance<IAchievementService>();
            var achievementPush = container.GetInstance<IAchievementPushService>();

            foreach (var user in await userPull.Pull())
            {
                var workouts = await workoutPull.Pull(user);
                var achievements = await achieveService.Process(user, workouts);
                await achievementPush.Push(achievements);
                _cancelSource.Token.ThrowIfCancellationRequested();
            }
        }
    }
}