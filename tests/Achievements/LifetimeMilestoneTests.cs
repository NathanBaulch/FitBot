using System;
using System.Linq;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using Moq;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    [TestFixture]
    public class LifetimeMilestoneTests
    {
        [Test]
        public void Normal_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 900000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 100000}}}}};

            var achievements = new LifetimeMilestoneProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("LifetimeMilestone"));
            Assert.That(achievement.Group, Is.EqualTo("Cycling"));
            Assert.That(achievement.Distance, Is.EqualTo(1000000M));
            Assert.That(achievement.CommentText, Is.EqualTo("Lifetime Cycling milestone: 1,000 km"));
        }

        [Test]
        public void NoPrevious_Test()
        {
            var database = new SQLiteDatabaseService();

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 100000}}}}};

            var achievements = new LifetimeMilestoneProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void BelowThreshold_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 800000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 100000}}}}};

            var achievements = new LifetimeMilestoneProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void NotEnough_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 1800000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 100000}}}}};

            var achievements = new LifetimeMilestoneProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements, Is.Empty);
        }
    }
}