using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using DapperExtensions.Sql;
using FitBot.Services;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    public abstract class BaseAchievementProviderTests
    {
        static BaseAchievementProviderTests()
        {
            SqlMapper.AddTypeHandler(new DecimalTypeHandler());
            SqlMapper.AddTypeHandler(new Int32TypeHandler());
            DapperExtensions.DapperExtensions.SqlDialect = new QuotedSqliteDialect();
        }

        [SetUp]
        public void SetUp()
        {
            File.Delete("FitBotTests.db");
            var setting = ConfigurationManager.ConnectionStrings["Default"];
            var factory = DbProviderFactories.GetFactory(setting.ProviderName);

            using (var con = factory.CreateConnection())
            {
                con.ConnectionString = setting.ConnectionString;
                con.Open();

                using (var cmd = con.CreateCommand())
                using (var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream("FitBot.Test.SQLite.sql")))
                {
                    cmd.CommandText = reader.ReadToEnd();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete("FitBotTests.db");
        }

        protected IDatabaseService CreateDatabase()
        {
            return new SqliteDatabaseService();
        }

        #region Nested type: DecimalTypeHandler

        private class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
        {
            public override decimal Parse(object value)
            {
                return Convert.ToDecimal(value);
            }

            public override void SetValue(IDbDataParameter parameter, decimal value)
            {
                parameter.Value = value;
            }
        }

        #endregion

        #region Nested type: Int32TypeHandler

        private class Int32TypeHandler : SqlMapper.TypeHandler<int>
        {
            public override int Parse(object value)
            {
                return Convert.ToInt32(value);
            }

            public override void SetValue(IDbDataParameter parameter, int value)
            {
                parameter.Value = value;
            }
        }

        #endregion

        #region Nested type: QuotedSqliteDialect

        private class QuotedSqliteDialect : SqliteDialect
        {
            public override string GetColumnName(string prefix, string columnName, string alias)
            {
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    throw new ArgumentNullException(columnName);
                }

                var name = QuoteString(columnName);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    name += " AS " + QuoteString(alias);
                }
                return name;
            }
        }

        #endregion

        #region Nested type: SqliteDatabaseService

        private class SqliteDatabaseService : DatabaseService, IDatabaseService
        {
            async Task<IEnumerable<T>> IDatabaseService.Query<T>(string sql, object parameters)
            {
                return (await Query<T>(TransformSql(sql), TransformParameters(parameters))).Select(TransformResult);
            }

            async Task<T> IDatabaseService.Single<T>(string sql, object parameters)
            {
                return TransformResult(await Single<T>(TransformSql(sql), TransformParameters(parameters)));
            }

            private static string TransformSql(string sql)
            {
                var match = Regex.Match(sql, @"select top ([@\w]+) (.*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return string.Format("select {0} limit {1}", match.Groups[2].Value, match.Groups[1].Value);
                }
                match = Regex.Match(sql, @"(.*) offset ([@\w]+) rows fetch next ([@\w]+) rows only", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return string.Format("{0} limit {1} offset {2}", match.Groups[1].Value, match.Groups[3].Value, match.Groups[2].Value);
                }
                return sql;
            }

            private static object TransformParameters(object parameters)
            {
                return parameters != null
                           ? parameters.GetType()
                                       .GetProperties()
                                       .ToDictionary(prop => prop.Name,
                                                     prop =>
                                                         {
                                                             var value = prop.GetValue(parameters, null);
                                                             return value is decimal ? Convert.ToDouble(value) : value;
                                                         })
                           : null;
            }

            private static T TransformResult<T>(T result)
            {
                var dict = result as IDictionary<string, object>;
                if (dict != null)
                {
                    foreach (var pair in dict.ToArray())
                    {
                        if (pair.Value is double)
                        {
                            dict[pair.Key] = Convert.ToDecimal(pair.Value);
                        }
                    }
                }
                return result;
            }
        }

        #endregion
    }
}