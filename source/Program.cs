using System;
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
            container.RegisterSingle<IScrapingService, ScrapingService>();
            container.RegisterSingle<IDatabaseService, DatabaseService>();
            container.RegisterSingle<IFitocracyService, DevFitocracyService>(); //TODO: switch to normal service
            container.RegisterSingle<IUserSyncService, UserSyncService>();
            container.RegisterSingle<IWorkoutSyncService, WorkoutSyncService>();
            container.RegisterSingle<IAchievementService, AchievementService>();
            container.RegisterAll<IAchievementProvider>(
                typeof (Program).Assembly
                                .GetTypes()
                                .Where(typeof (IAchievementProvider).IsAssignableFrom)
                                .Where(type => type.IsClass));
            container.Verify();

            container.GetInstance<IUserSyncService>().Execute().Wait();
            container.GetInstance<IDatabaseService>().Insert(new User {Id = 135115, Username = "NathanBaulch"});
            container.GetInstance<IWorkoutSyncService>().Execute().Wait();
            container.GetInstance<IAchievementService>().Execute().Wait();

            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }

        private static async Task ProcessUser(Container container, long userId, string username)
        {
            var database = container.GetInstance<IDatabaseService>();
            var user = await database.Single<User>(
                "select * " +
                "from [User] " +
                "where [Id] = @userId", new {userId});
            if (user == null)
            {
                user = new User
                    {
                        Id = userId,
                        Username = username
                    };
                database.Insert(user);
            }

            await container.GetInstance<IWorkoutSyncService>().Execute();

            user.DirtyDate = new DateTime(2010, 1, 1);
            database.Update(user);

            await container.GetInstance<IAchievementService>().Execute();
        }
    }
}