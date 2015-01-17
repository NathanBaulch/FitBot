using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using SimpleInjector;
using SimpleInjector.Extensions;

namespace FitBot
{
    internal static class Program
    {
        private static void Main()
        {
            var container = new Container();

            container.RegisterSingle<IAchievementPushService, AchievementPushService>();
            container.RegisterSingle<IAchievementService, AchievementService>();
            container.RegisterSingle<IActivityGroupingService, ActivityGroupingService>();
            container.RegisterSingle<IDatabaseService, DatabaseService>();
            container.RegisterSingle<IFitocracyService, FitocracyService>();
            container.RegisterSingle<IScrapingService, ScrapingService>();
            container.RegisterSingle<IUserPullService, UserPullService>();
            container.RegisterSingle<IWebRequestService, WebRequestService>();
            container.RegisterSingle<IWorkoutPullService, WorkoutPullService>();

            container.RegisterAll<IAchievementProvider>(
                typeof (Program).Assembly
                                .GetTypes()
                                .Where(typeof (IAchievementProvider).IsAssignableFrom)
                                .Where(type => type.IsClass));

            container.RegisterSingleDecorator(typeof (IWebRequestService), typeof (ThrottledWebRequestDecorator));

            container.Verify();

            var throttler = (ThrottledWebRequestDecorator) container.GetInstance<IWebRequestService>();

            while (true)
            {
                var watch = Stopwatch.StartNew();
                Execute(container).Wait();
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

        private static async Task Execute(Container container)
        {
            var userPull = container.GetInstance<IUserPullService>();
            var workoutPull = container.GetInstance<IWorkoutPullService>();
            var achievements = container.GetInstance<IAchievementService>();
            var achievementPush = container.GetInstance<IAchievementPushService>();
            var tasks = new List<Task<IEnumerable<Achievement>>>();

            foreach (var user in await userPull.Pull())
            {
                var workouts = await workoutPull.Pull(user);
                tasks.Add(achievements.Process(user, workouts));
            }

            while (tasks.Count > 0)
            {
                var task = await Task.WhenAny(tasks);
                await achievementPush.Push(task.Result);
                tasks.Remove(task);
            }
        }
    }
}