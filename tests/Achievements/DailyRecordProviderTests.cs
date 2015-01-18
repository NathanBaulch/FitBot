﻿using System;
using System.Linq;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using Moq;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    [TestFixture]
    public class DailyRecordProviderTests : BaseAchievementProviderTests
    {
        [Test]
        public void Normal_Test()
        {
            var database = CreateDatabase();

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
            var database = CreateDatabase();

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}};

            var achievements = new DailyRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements.Any(), Is.False);
        }

        [Test]
        public void Previous_Single_Set_Record_Test()
        {
            var database = CreateDatabase();
            database.Insert(new Workout {Id = 0, Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 1000}, new Set {Distance = 1000, Sequence = 1}}}}});
            database.Insert(new Workout {Id = 1, Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 5000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = DateTime.Today, Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 2000}, new Set {Distance = 2000}}}}};

            var achievements = new DailyRecordProvider(database, activityGrouping.Object).Execute(workout).Result;

            Assert.That(achievements.Any(), Is.False);
        }
    }
}