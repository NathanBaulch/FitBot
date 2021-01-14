using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using DapperExtensions.Sql;
using FitBot.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitBot.Test
{
    public class SQLiteDatabaseService : DatabaseService
    {
        static SQLiteDatabaseService()
        {
            DbProviderFactories.RegisterFactory("System.Data.SQLite", SQLiteFactory.Instance);

            SqlMapper.AddTypeHandler(new DecimalTypeHandler());
            SqlMapper.AddTypeHandler(new Int32TypeHandler());
            DapperExtensions.DapperExtensions.SqlDialect = new QuotedSqliteDialect();
        }

        public SQLiteDatabaseService()
            : this("FitBot.Test.db")
        {
        }

        private SQLiteDatabaseService(string fileName)
            : this(fileName, "System.Data.SQLite", $"Data Source={fileName};Version=3")
        {
        }

        private SQLiteDatabaseService(string fileName, string providerName, string connectionString)
            : base(NullLogger<DatabaseService>.Instance, new DatabaseOptions {ProviderName = providerName, ConnectionString = connectionString})
        {
            File.Delete(fileName);
            var factory = DbProviderFactories.GetFactory(providerName);

            using var con = factory.CreateConnection();
            con.ConnectionString = connectionString;
            con.Open();

            using var cmd = con.CreateCommand();
            using var reader = new StreamReader(GetType().Assembly.GetManifestResourceStream("FitBot.Test.SQLite.sql"));
            cmd.CommandText = reader.ReadToEnd();
            cmd.ExecuteNonQuery();
        }

        public override IEnumerable<T> Query<T>(string sql, object parameters = null) => base.Query<T>(TransformSql(sql), TransformParameters(parameters)).Select(TransformResult);

        public override T Single<T>(string sql, object parameters) => TransformResult(base.Single<T>(TransformSql(sql), TransformParameters(parameters)));

        private static string TransformSql(string sql)
        {
            var match = Regex.Match(sql, @"select top ([@\w]+) (.*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"select {match.Groups[2].Value} limit {match.Groups[1].Value}";
            }
            match = Regex.Match(sql, @"(.*) offset ([@\w]+) rows fetch next ([@\w]+) rows only", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return $"{match.Groups[1].Value} limit {match.Groups[3].Value} offset {match.Groups[2].Value}";
            }
            return sql;
        }

        private static object TransformParameters(object parameters) =>
            parameters?.GetType()
                .GetProperties()
                .ToDictionary(prop => prop.Name,
                    prop =>
                    {
                        var value = prop.GetValue(parameters, null);
                        return value is decimal ? Convert.ToDouble(value) : value;
                    });

        private static T TransformResult<T>(T result)
        {
            if (result is IDictionary<string, object> dict)
            {
                foreach (var (key, value) in dict.ToArray())
                {
                    if (value is double)
                    {
                        dict[key] = Convert.ToDecimal(value);
                    }
                }
            }
            return result;
        }

        #region Nested type: DecimalTypeHandler

        private class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
        {
            public override decimal Parse(object value) => Convert.ToDecimal(value);

            public override void SetValue(IDbDataParameter parameter, decimal value) => parameter.Value = value;
        }

        #endregion

        #region Nested type: Int32TypeHandler

        private class Int32TypeHandler : SqlMapper.TypeHandler<int>
        {
            public override int Parse(object value) => Convert.ToInt32(value);

            public override void SetValue(IDbDataParameter parameter, int value) => parameter.Value = value;
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
    }
}