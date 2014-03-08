using System;
using FitBot.Services;
using SimpleInjector;

namespace FitBot
{
    internal static class Program
    {
        private static void Main()
        {
            var container = new Container();
            container.Register<IScrapingService, ScrapingService>(Lifestyle.Singleton);
            container.Register<IDatabaseService, DatabaseService>(Lifestyle.Singleton);
            container.Register<IFitocracyService, CachedFitocracyService>(Lifestyle.Singleton); //TODO: switch to normal service
            container.Register<IUserSyncService, UserSyncService>(Lifestyle.Singleton);
            container.Register<IWorkoutSyncService, WorkoutSyncService>(Lifestyle.Singleton);
            container.Verify();

            var userSync = container.GetInstance<IUserSyncService>();
            var workoutSync = container.GetInstance<IWorkoutSyncService>();

            userSync.Run().Wait();
            workoutSync.Run().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}