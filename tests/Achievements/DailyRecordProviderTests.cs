using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using Dapper;
using DapperExtensions.Sql;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using Moq;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    [TestFixture]
    public class DailyRecordProviderTests
    {
        static DailyRecordProviderTests()
        {
            SqlMapper.AddTypeHandler(new DecimalTypeHandler());
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
                {
                    using (var stream = new StreamReader(GetType().Assembly.GetManifestResourceStream("FitBot.Test.SQLite.sql")))
                    {
                        cmd.CommandText = stream.ReadToEnd();
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete("FitBotTests.db");
        }

        [Test]
        public void Normal_Test()
        {
            var database = new DatabaseService();

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 1000}, new Set {Distance = 2000}}}}};

            var achievements = new DailyRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("DailyRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Cycling"));
            Assert.That(achievement.Distance, Is.EqualTo(3000M));
            Assert.That(achievement.CommentText, Is.EqualTo("Daily Cycling record: 3 km"));
        }

        [Test]
        public void Only_Single_Set_Test()
        {
            var database = new DatabaseService();

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}};

            var achievements = new DailyRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements.Any(), Is.False);
        }

        [Test]
        public void Previous_Single_Set_Record_Test()
        {
            var database = new DatabaseService();
            database.Insert(new Workout {Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 1000}, new Set {Distance = 1000, Sequence = 1}}}}});
            database.Insert(new Workout {Id = 1, Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 5000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = DateTime.Today, Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 2000}, new Set {Distance = 2000}}}}};

            var achievements = new DailyRecordProvider(database, activityGrouping.Object).Execute(workout).Result;

            Assert.That(achievements.Any(), Is.False);
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