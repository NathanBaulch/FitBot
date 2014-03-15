using System;
using System.Linq;
using FitBot.Achievements;
using FitBot.Services;
using SimpleInjector;

//TODO: should set data be summarized at the activity level? SetCount, Points, Distance, Duration, Repetitions
//TODO: unit test for new user that logs something new during initial or update sync

namespace FitBot
{
    internal static class Program
    {
        private static void Main()
        {
            var container = new Container();
            container.RegisterSingle<IScrapingService, ScrapingService>();
            container.RegisterSingle<IDatabaseService, DatabaseService>();
            container.RegisterSingle<IFitocracyService, DevFitocracyService>(); //TODO: switch to normal service
            container.RegisterSingle<IUserSyncService, UserSyncService>();
            container.RegisterSingle<IWorkoutSyncService, WorkoutSyncService>();
            container.RegisterSingle<IActivityGroupingService, ActivityGroupingService>();
            container.RegisterSingle<IAchievementService, AchievementService>();
            container.RegisterAll<IAchievementProvider>(
                typeof (Program).Assembly
                                .GetTypes()
                                .Where(typeof (IAchievementProvider).IsAssignableFrom)
                                .Where(type => type.IsClass));
            container.Verify();

            container.GetInstance<IUserSyncService>().Execute().Wait();
            container.GetInstance<IWorkoutSyncService>().Execute().Wait();
            container.GetInstance<IAchievementService>().Execute().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}