using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;
using FitBot.Model;
using Activity = FitBot.Model.Activity;

namespace FitBot.Services
{
    public record DatabaseOptions
    {
        public string ProviderName { get; init; }
        public string ConnectionString { get; init; }
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly DbProviderFactory _factory;
        private readonly string _connectionString;

        private readonly IncludablePropertyMap _userInsertDateProp;
        private readonly IncludablePropertyMap _workoutInsertDateProp;
        private readonly IncludablePropertyMap _achievementInsertDateProp;

        public DatabaseService(DatabaseOptions options)
        {
            _factory = DbProviderFactories.GetFactory(options.ProviderName);
            _connectionString = options.ConnectionString;

            DapperExtensions.DapperExtensions.DefaultMapper = typeof(IncludableClassMapper<>);
            GetPropertyMap<User>(x => x.Id).Key(KeyType.Assigned);
            GetPropertyMap<Workout>(x => x.Id).Key(KeyType.Assigned);
            GetPropertyMap<Workout>(x => x.State).Ignore();
            GetPropertyMap<Workout>(x => x.Activities).Ignore();
            GetPropertyMap<Workout>(x => x.Comments).Ignore();
            GetPropertyMap<Activity>(x => x.Sets).Ignore();
            _userInsertDateProp = GetPropertyMap<User>(x => x.InsertDate);
            _workoutInsertDateProp = GetPropertyMap<Workout>(x => x.InsertDate);
            _achievementInsertDateProp = GetPropertyMap<Achievement>(x => x.InsertDate);
        }

        public virtual async Task<IEnumerable<T>> Query<T>(string sql, object parameters = null)
        {
            using var con = OpenConnection();
            return await con.QueryAsync<T>(sql, parameters);
        }

        public virtual async Task<T> Single<T>(string sql, object parameters)
        {
            using var con = OpenConnection();
            return await con.QuerySingleOrDefaultAsync<T>(sql, parameters);
        }

        public Task<IEnumerable<User>> GetUsers() =>
            Query<User>(
                "select * " +
                "from [User] " +
                "order by [Id]");

        public void Insert(User user)
        {
            Trace.TraceInformation("Insert user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            _userInsertDateProp.Include();
            user.InsertDate = DateTime.UtcNow;
            con.Insert(user);
        }

        public void Update(User user)
        {
            Trace.TraceInformation("Update user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            _userInsertDateProp.Ignore();
            user.UpdateDate = DateTime.UtcNow;
            con.Update(user);
        }

        public void Delete(User user)
        {
            Trace.TraceInformation("Delete user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            con.Delete(user);
        }

        public Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime fromDate, DateTime toDate) =>
            Query<Workout>(
                "select * " +
                "from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] >= @fromDate " +
                "and [Date] < @toDate " +
                "order by [Date], [Id]", new {userId, fromDate, toDate});

        public Task<IEnumerable<long>> GetUnresolvedWorkoutIds(long userId, DateTime after) =>
            Query<long>(
                "select [Id] " +
                "from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] > @after " +
                "and [Id] in (" +
                "  select [WorkoutId] " +
                "  from [Achievement] " +
                "  where [CommentText] is not null " +
                "  and [CommentId] is null)" +
                "order by [Id]", new {userId, after});

        public void DeleteWorkouts(long userId, DateTime before)
        {
            Trace.TraceInformation("Delete workouts for user {0} before {1}", userId, before);
            using var con = OpenConnection();
            con.Execute(
                "delete from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] < @before", new {userId, before});
        }

        public void Insert(Workout workout)
        {
            Trace.TraceInformation("Insert workout " + workout.Id);
            using var con = OpenConnection();
            using var trans = con.BeginTransaction();
            _workoutInsertDateProp.Include();
            workout.InsertDate = DateTime.UtcNow;
            con.Insert(workout, trans);
            InsertWorkoutActivities(workout, con, trans);
            trans.Commit();
        }

        public void Update(Workout workout, bool deep)
        {
            Trace.TraceInformation("Update workout {0} ({1})", workout.Id, deep ? "deep" : "shallow");
            using var con = OpenConnection();
            _workoutInsertDateProp.Ignore();
            workout.UpdateDate = DateTime.UtcNow;
            if (deep)
            {
                using var trans = con.BeginTransaction();
                con.Update(workout, trans);
                con.Execute(
                    "delete from [Activity] " +
                    "where [WorkoutId] = @Id", new {workout.Id}, trans);
                InsertWorkoutActivities(workout, con, trans);
                trans.Commit();
            }
            else
            {
                con.Update(workout);
            }
        }

        public void Delete(Workout workout)
        {
            Trace.TraceInformation("Delete workout " + workout.Id);
            using var con = OpenConnection();
            con.Delete(workout);
        }

        public Task<IEnumerable<Achievement>> GetAchievements(long workoutId) =>
            Query<Achievement>(
                "select * " +
                "from [Achievement] " +
                "where [WorkoutId] = @workoutId " +
                "order by [Type], [Group], [Id]", new {workoutId});

        public void Insert(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                Trace.TraceInformation("Insert achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                Trace.TraceInformation("Insert achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                Trace.TraceInformation("Insert achievement " + achievement.Type);
            }

            using var con = OpenConnection();
            _achievementInsertDateProp.Include();
            achievement.InsertDate = DateTime.UtcNow;
            con.Insert(achievement);
        }

        public void Update(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                Trace.TraceInformation("Update achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                Trace.TraceInformation("Update achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                Trace.TraceInformation("Update achievement " + achievement.Type);
            }

            using var con = OpenConnection();
            _achievementInsertDateProp.Ignore();
            achievement.UpdateDate = DateTime.UtcNow;
            con.Update(achievement);
        }

        public void UpdateCommentId(long achievementId, long commentId)
        {
            Trace.TraceInformation("Update comment ID on achievement {0}", achievementId);
            using var con = OpenConnection();
            con.Execute(
                "update [Achievement] " +
                "set [CommentId] = @commentId " +
                "where [Id] = @achievementId", new {commentId, achievementId});
        }

        public void Delete(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                Trace.TraceInformation("Delete achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                Trace.TraceInformation("Delete achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                Trace.TraceInformation("Delete achievement " + achievement.Type);
            }

            using var con = OpenConnection();
            con.Delete(achievement);
        }

        private static IncludablePropertyMap GetPropertyMap<T>(Expression<Func<T, object>> expression)
            where T : class
        {
            var member = ReflectionHelper.GetProperty(expression);
            return DapperExtensions.DapperExtensions
                .GetMap<T>()
                .Properties
                .Cast<IncludablePropertyMap>()
                .First(prop => prop.PropertyInfo == member);
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

        #region Nested type: IncludableClassMapper

        private class IncludableClassMapper<T> : AutoClassMapper<T>
            where T : class
        {
            protected override void AutoMap()
            {
                base.AutoMap();
                var props = Properties.Select(prop => new IncludablePropertyMap(prop)).ToList();
                Properties.Clear();
                foreach (var prop in props)
                {
                    Properties.Add(prop);
                }
            }
        }

        #endregion

        #region Nested type: IncludablePropertyMap

        private class IncludablePropertyMap : IPropertyMap
        {
            public IncludablePropertyMap(IPropertyMap map)
            {
                Name = map.Name;
                ColumnName = map.ColumnName;
                Ignored = map.Ignored;
                IsReadOnly = map.IsReadOnly;
                KeyType = map.KeyType;
                PropertyInfo = map.PropertyInfo;
            }

            public string Name { get; }
            public string ColumnName { get; }
            public bool Ignored { get; private set; }
            public bool IsReadOnly { get; }
            public KeyType KeyType { get; private set; }
            public PropertyInfo PropertyInfo { get; }

            public void Key(KeyType keyType) => KeyType = keyType;

            public void Include() => Ignored = false;

            public void Ignore() => Ignored = true;
        }

        #endregion
    }
}