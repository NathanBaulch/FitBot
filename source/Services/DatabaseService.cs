using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;
using DapperExtensions;
using DapperExtensions.Mapper;
using DapperExtensions.Sql;
using FitBot.Model;
using Microsoft.Extensions.Logging;
using Npgsql;
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
        private readonly ILogger<DatabaseService> _logger;
        private readonly DbProviderFactory _factory;
        private readonly string _connectionString;

        private readonly IncludablePropertyMap _userInsertDateProp;
        private readonly IncludablePropertyMap _workoutInsertDateProp;
        private readonly IncludablePropertyMap _workoutUserIdProp;
        private readonly IncludablePropertyMap _achievementInsertDateProp;
        private readonly IncludablePropertyMap _achievementWorkoutIdProp;

        public DatabaseService(ILogger<DatabaseService> logger, DatabaseOptions options)
        {
            _logger = logger;

            switch (options.ProviderName)
            {
                case "System.Data.SqlClient":
                    DbProviderFactories.RegisterFactory("System.Data.SqlClient", SqlClientFactory.Instance);
                    DapperExtensions.DapperExtensions.SqlDialect = new SqlServerDialect();
                    break;
                case "Npgsql":
                    DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
                    DapperExtensions.DapperExtensions.SqlDialect = new PostgreSqlDialect();
                    break;
            }

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
            _workoutUserIdProp = GetPropertyMap<Workout>(x => x.UserId);
            _achievementInsertDateProp = GetPropertyMap<Achievement>(x => x.InsertDate);
            _achievementWorkoutIdProp = GetPropertyMap<Achievement>(x => x.WorkoutId);
        }

        public virtual IEnumerable<T> Query<T>(string sql, object parameters = null, int limit = 0)
        {
            using var con = OpenConnection();
            return con.Query<T>(Prepare(sql, limit, ref parameters), parameters);
        }

        public virtual T Single<T>(string sql, object parameters, bool limit = false)
        {
            using var con = OpenConnection();
            return con.QuerySingleOrDefault<T>(Prepare(sql, limit ? 1 : 0, ref parameters), parameters);
        }

        public IEnumerable<User> GetUsers() =>
            Query<User>(
                "select * " +
                "from [User] " +
                "order by [Id]");

        public User GetUser(long id) =>
            Single<User>(
                "select * " +
                "from [User] " +
                "where [Id] = @id", new {id});

        public void Insert(User user)
        {
            _logger.LogDebug("Insert user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            _userInsertDateProp.Include();
            user.InsertDate = DateTime.UtcNow;
            con.Insert(user);
        }

        public void Update(User user)
        {
            _logger.LogDebug("Update user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            _userInsertDateProp.Ignore();
            user.UpdateDate = DateTime.UtcNow;
            con.Update(user);
        }

        public void Delete(User user)
        {
            _logger.LogDebug("Delete user {0} ({1})", user.Id, user.Username);
            using var con = OpenConnection();
            con.Delete(user);
        }

        public IEnumerable<Workout> GetWorkouts(long userId, DateTime? fromDate, DateTime? toDate) =>
            Query<Workout>(
                "select * " +
                "from [Workout] " +
                "where [UserId] = @userId " +
                (fromDate != null ? "and [Date] >= @fromDate " : "") +
                (toDate != null ? "and [Date] < @toDate " : "") +
                "order by [Date], [Id]", new {userId, fromDate, toDate});

        public IEnumerable<long> GetUnresolvedWorkoutIds(long userId, DateTime after) =>
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
            _logger.LogDebug("Delete workouts for user {0} before {1}", userId, before);
            using var con = OpenConnection();
            con.Execute(Quote(
                "delete from [Workout] " +
                "where [UserId] = @userId " +
                "and [Date] < @before"), new {userId, before});
        }

        public void Insert(Workout workout)
        {
            _logger.LogDebug("Insert workout " + workout.Id);
            using var con = OpenConnection();
            using var trans = con.BeginTransaction();
            _workoutInsertDateProp.Include();
            _workoutUserIdProp.Include();
            workout.InsertDate = DateTime.UtcNow;
            con.Insert(workout, trans);
            InsertWorkoutActivities(workout, con, trans);
            trans.Commit();
        }

        public void Update(Workout workout, bool deep)
        {
            _logger.LogDebug("Update workout {0} ({1})", workout.Id, deep ? "deep" : "shallow");
            using var con = OpenConnection();
            _workoutInsertDateProp.Ignore();
            _workoutUserIdProp.Ignore();
            workout.UpdateDate = DateTime.UtcNow;
            if (deep)
            {
                using var trans = con.BeginTransaction();
                con.Update(workout, trans);
                con.Execute(Quote(
                    "delete from [Activity] " +
                    "where [WorkoutId] = @Id"), new {workout.Id}, trans);
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
            _logger.LogDebug("Delete workout " + workout.Id);
            using var con = OpenConnection();
            con.Delete(workout);
        }

        public IEnumerable<Activity> GetActivities(long workoutId)
        {
            var sets = Query<Set>(
                    "select * " +
                    "from [Set] " +
                    "where [ActivityId] in (select [Id] from [Activity] where [WorkoutId] = @workoutId) " +
                    "order by [Sequence]", new {workoutId})
                .ToLookup(s => s.ActivityId);
            return Query<Activity>(
                    "select * " +
                    "from [Activity] " +
                    "where [WorkoutId] = @workoutId " +
                    "order by [Sequence]", new {workoutId})
                .Select(activity =>
                {
                    activity.Sets = sets[activity.Id].ToList();
                    return activity;
                });
        }

        public IEnumerable<Achievement> GetAchievements(long workoutId) =>
            Query<Achievement>(
                "select * " +
                "from [Achievement] " +
                "where [WorkoutId] = @workoutId " +
                "order by [Type], [Group], [Id]", new {workoutId});

        public void Insert(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                _logger.LogDebug("Insert achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                _logger.LogDebug("Insert achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                _logger.LogDebug("Insert achievement " + achievement.Type);
            }

            using var con = OpenConnection();
            _achievementInsertDateProp.Include();
            _achievementWorkoutIdProp.Include();
            achievement.InsertDate = DateTime.UtcNow;
            con.Insert(achievement);
        }

        public void Update(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                _logger.LogDebug("Update achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                _logger.LogDebug("Update achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                _logger.LogDebug("Update achievement " + achievement.Type);
            }

            using var con = OpenConnection();
            _achievementInsertDateProp.Ignore();
            _achievementWorkoutIdProp.Ignore();
            achievement.UpdateDate = DateTime.UtcNow;
            con.Update(achievement);
        }

        public void UpdateCommentId(long achievementId, long commentId)
        {
            _logger.LogDebug("Update comment ID on achievement {0}", achievementId);
            using var con = OpenConnection();
            con.Execute(Quote(
                "update [Achievement] " +
                "set [CommentId] = @commentId " +
                "where [Id] = @achievementId"), new {commentId, achievementId});
        }

        public void Delete(Achievement achievement)
        {
            if (achievement.Group != null)
            {
                _logger.LogDebug("Delete achievement {0} for group {1}", achievement.Type, achievement.Group);
            }
            else if (achievement.Activity != null)
            {
                _logger.LogDebug("Delete achievement {0} for activity {1}", achievement.Type, achievement.Activity);
            }
            else
            {
                _logger.LogDebug("Delete achievement " + achievement.Type);
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

        private static string Prepare(string sql, int limit, ref object parameters)
        {
            sql = Quote(sql);

            if (limit > 0)
            {
                var newParams = new Dictionary<string, object>();
                sql = DapperExtensions.DapperExtensions.SqlDialect.GetPagingSql(sql, 0, limit, newParams);
                if (newParams.Count > 0)
                {
                    var dynParams = new DynamicParameters(parameters);
                    dynParams.AddDynamicParams(newParams);
                    parameters = dynParams;
                }
            }

            return sql;
        }

        private static string Quote(string sql)
        {
            if (DapperExtensions.DapperExtensions.SqlDialect.OpenQuote != '[')
            {
                sql = sql.Replace('[', DapperExtensions.DapperExtensions.SqlDialect.OpenQuote);
            }

            if (DapperExtensions.DapperExtensions.SqlDialect.CloseQuote != ']')
            {
                sql = sql.Replace(']', DapperExtensions.DapperExtensions.SqlDialect.CloseQuote);
            }

            return sql;
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

        #region Nested type: PostgreSqlDialect

        private class PostgreSqlDialect : SqlDialectBase
        {
            public override string GetIdentitySql(string tableName)
            {
                return "SELECT LASTVAL() AS Id";
            }

            public override string GetPagingSql(string sql, int page, int resultsPerPage, IDictionary<string, object> parameters)
            {
                return GetSetSql(sql, page * resultsPerPage, resultsPerPage, parameters);
            }

            public override string GetSetSql(string sql, int pageNumber, int maxResults, IDictionary<string, object> parameters)
            {
                sql += " LIMIT @maxResults OFFSET @pageStartRowNbr";
                parameters.Add("@maxResults", maxResults);
                parameters.Add("@pageStartRowNbr", pageNumber * maxResults);
                return sql;
            }

            public override string GetColumnName(string prefix, string columnName, string alias)
            {
                return base.GetColumnName(null, columnName, alias);
            }
        }

        #endregion
    }
}