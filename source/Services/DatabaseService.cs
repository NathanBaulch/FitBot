﻿using System;
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
//TODO: consider async crud

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

        public Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime fromDate, DateTime toDate)
        {
            return Query<Workout>(
                "select * " +
                "from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] >= @fromDate " +
                "and [Date] < @toDate " +
                "order by [Date]", new {userId, fromDate, toDate});
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

        public void Insert(Workout workout)
        {
            Debug.WriteLine("Inserting workout " + workout.Id);
            using (var con = OpenConnection())
            using (var trans = con.BeginTransaction())
            {
                con.Insert(workout, trans);
                InsertWorkoutActivities(workout, con, trans);
                trans.Commit();
            }
        }

        public void Update(Workout workout, bool deep)
        {
            Debug.WriteLine("Updating workout " + workout.Id + (deep ? " deep" : " shallow"));
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

        public void Insert(Achievement achievement)
        {
            Debug.WriteLine("Inserting achievement " + achievement.Type + " for group " + achievement.Group);
            using (var con = OpenConnection())
            {
                con.Insert(achievement);
            }
        }

        public void Update(Achievement achievement)
        {
            Debug.WriteLine("Updating achievement " + achievement.Type + " for group " + achievement.Group);
            using (var con = OpenConnection())
            {
                con.Update(achievement);
            }
        }

        public void Delete(Achievement achievement)
        {
            Debug.WriteLine("Deleting achievement " + achievement.Type + " for group " + achievement.Group);
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