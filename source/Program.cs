﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using SimpleInjector;

namespace FitBot
{
    internal static class Program
    {
        private static void Main()
        {
            var container = new Container();

            container.RegisterSingleton<IAchievementPushService, AchievementPushService>();
            container.RegisterSingleton<IAchievementService, AchievementService>();
            container.RegisterSingleton<IActivityGroupingService, ActivityGroupingService>();
            container.RegisterSingleton<IDatabaseService, DatabaseService>();
            container.RegisterSingleton<IFitocracyService, FitocracyService>();
            container.RegisterSingleton<IScrapingService, ScrapingService>();
            container.RegisterSingleton<IUserPullService, UserPullService>();
            container.RegisterSingleton<IWebRequestService, WebRequestService>();
            container.RegisterSingleton<IWorkoutPullService, WorkoutPullService>();

            container.RegisterCollection<IAchievementProvider>(
                typeof (Program).Assembly
                                .GetTypes()
                                .Where(typeof (IAchievementProvider).IsAssignableFrom)
                                .Where(type => type.IsClass));

            container.RegisterDecorator<IWebRequestService, ThrottledWebRequestDecorator>(Lifestyle.Singleton);

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