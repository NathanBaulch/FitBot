using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;
using FitBot.Model;

//TODO: possible connectivity problems
//TODO: consider batch insertion

namespace FitBot.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly DbProviderFactory _factory;
        private readonly string _connectionString;

        public DatabaseService()
        {
            var setting = ConfigurationManager.ConnectionStrings["Default"];
            _factory = DbProviderFactories.GetFactory(setting.ProviderName);
            _connectionString = setting.ConnectionString;

            var userProps = DapperExtensions.DapperExtensions.GetMap<User>().Properties;
            ((PropertyMap) userProps.First(prop => prop.ColumnName == "Id")).Key(KeyType.Assigned);
            var workoutProps = DapperExtensions.DapperExtensions.GetMap<Workout>().Properties;
            ((PropertyMap) workoutProps.First(prop => prop.ColumnName == "Id")).Key(KeyType.Assigned);
            ((PropertyMap) workoutProps.First(prop => prop.ColumnName == "Activities")).Ignore();
            var activityProps = DapperExtensions.DapperExtensions.GetMap<Activity>().Properties;
            ((PropertyMap) activityProps.First(prop => prop.ColumnName == "Sets")).Ignore();
        }

        public async Task<IEnumerable<T>> Query<T>(string sql, object parameters = null)
        {
            using (var con = OpenConnection())
            {
                return await con.QueryAsync<T>(sql, parameters);
            }
        }

        public async Task<T> Single<T>(string sql, object parameters)
        {
            using (var con = OpenConnection())
            {
                return (await con.QueryAsync<T>(sql, parameters)).SingleOrDefault();
            }
        }

        public Task<IEnumerable<User>> GetUsers()
        {
            return Query<User>(
                "select * " +
                "from [User] " +
                "order by [Id]");
        }

        public Task<IEnumerable<User>> GetUsersWithDirtyDate()
        {
            return Query<User>(
                "select * " +
                "from [User] " +
                "where [DirtyDate] is not null " +
                "order by [Id]");
        }

        public void Insert(User user)
        {
            Debug.WriteLine("Inserting user " + user.Id);
            using (var con = OpenConnection())
            {
                con.Insert(user);
            }
        }

        public void Update(User user)
        {
            Debug.WriteLine("Updating user " + user.Id);
            using (var con = OpenConnection())
            {
                con.Update(user);
            }
        }

        public void Delete(User user)
        {
            Debug.WriteLine("Deleting user " + user.Id);
            using (var con = OpenConnection())
            {
                con.Delete(user);
            }
        }

        public Task<int> GetWorkoutCount(long userId)
        {
            return Single<int>(
                "select count(*) " +
                "from [Workout] " +
                "where [UserId] = @userId", new {userId});
        }

        public async Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime fromDate, DateTime toDate, bool deep)
        {
            if (deep)
            {
                using (var con = OpenConnection())
                {
                    return (await con.QueryAsync<Workout, Activity, Set, Tuple<Workout, Activity, Set>>(
                        "select * " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @userId " +
                        "and w.[Date] >= @fromDate " +
                        "and w.[Date] < @toDate " +
                        "order by w.[Date], w.[Id], a.[Sequence], s.[Sequence]",
                        (w, a, s) => new Tuple<Workout, Activity, Set>(w, a, s),
                        new {userId, fromDate, toDate}))
                        .GroupBy(tuple => tuple.Item1.Id)
                        .Select(workoutGroup =>
                            {
                                var workout = workoutGroup.First().Item1;
                                workout.Activities = workoutGroup
                                    .GroupBy(tuple => tuple.Item2.Id)
                                    .Select(activityGroup =>
                                        {
                                            var activity = activityGroup.First().Item2;
                                            activity.Sets = activityGroup.Select(item => item.Item3).ToList();
                                            return activity;
                                        })
                                    .ToList();
                                return workout;
                            });
                }
            }

            return await Query<Workout>(
                "select * " +
                "from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] >= @fromDate " +
                "and [Date] < @toDate " +
                "order by [Date], [Id]", new {userId, fromDate, toDate});
        }

        public void DeleteWorkoutsBefore(DateTime date)
        {
            Debug.WriteLine("Deleting workouts before " + date);
            using (var con = OpenConnection())
            {
                con.Execute(
                    "delete from [Workout] " +
                    "where [Date] < @date", new {date});
            }
        }

        public void Insert(Workout workout, bool deep)
        {
            Debug.WriteLine("Inserting workout {0} ({1})", workout.Id, deep ? "deep" : "shallow");
            using (var con = OpenConnection())
            {
                if (deep)
                {
                    using (var trans = con.BeginTransaction())
                    {
                        con.Insert(workout, trans);
                        InsertWorkoutActivities(workout, con, trans);
                        trans.Commit();
                    }
                }
                else
                {
                    con.Insert(workout);
                }
            }
        }

        public void Update(Workout workout, bool deep)
        {
            Debug.WriteLine("Updating workout {0} ({1})", workout.Id, deep ? "deep" : "shallow");
            using (var con = OpenConnection())
            {
                if (deep)
                {
                    using (var trans = con.BeginTransaction())
                    {
                        con.Update(workout, trans);
                        con.Execute(
                            "delete from [Activity] " +
                            "where [WorkoutId] = @Id", new {workout.Id}, trans);
                        InsertWorkoutActivities(workout, con, trans);
                        trans.Commit();
                    }
                }
                else
                {
                    con.Update(workout);
                }
            }
        }

        public void Delete(Workout workout)
        {
            Debug.WriteLine("Deleting workout " + workout.Id);
            using (var con = OpenConnection())
            {
                con.Delete(workout);
            }
        }

        public async Task<IEnumerable<Achievement>> GetAchievements(long workoutId)
        {
            return await Query<Achievement>(
                "select * " +
                "from [Achievement] " +
                "where [WorkoutId] = @workoutId " +
                "order by [Type], [Group]", new {workoutId});
        }

        public void Insert(Achievement achievement)
        {
            Debug.WriteLine("Inserting achievement {0} for group {1}", achievement.Type, achievement.Group);
            using (var con = OpenConnection())
            {
                con.Insert(achievement);
            }
        }

        public void Update(Achievement achievement)
        {
            Debug.WriteLine("Updating achievement {0} for group {1}", achievement.Type, achievement.Group);
            using (var con = OpenConnection())
            {
                con.Update(achievement);
            }
        }

        public void Delete(Achievement achievement)
        {
            Debug.WriteLine("Deleting achievement {0} for group {1}", achievement.Type, achievement.Group);
            using (var con = OpenConnection())
            {
                con.Delete(achievement);
            }
        }

        private IDbConnection OpenConnection()
        {
            var con = _factory.CreateConnection();
            con.ConnectionString = _connectionString;
            con.Open();
            return con;
        }

        private static void InsertWorkoutActivities(Workout workout, IDbConnection con, IDbTransaction trans)
        {
            foreach (var activity in workout.Activities)
            {
                activity.WorkoutId = workout.Id;
                con.Insert(activity, trans);
                foreach (var set in activity.Sets)
                {
                    set.ActivityId = activity.Id;
                    con.Insert(set, trans);
                }
            }
        }
    }
}