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
    public class QualifiedRecordProviderTests : BaseAchievementProviderTests
    {
        [Test]
        public void Normal_Test()
        {
            var database = CreateDatabase();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000, Speed = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 1000, Speed = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("QualifiedRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Cycling"));
            Assert.That(achievement.Speed, Is.EqualTo(2M));
            Assert.That(achievement.Distance, Is.EqualTo(1000M));
            Assert.That(achievement.CommentText, Is.EqualTo("Qualified Cycling record: 7.2 km/h for 1 km or more"));
        }

        [Test]
        public void Very_Small_Distance_Test()
        {
            var database = CreateDatabase();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000, Speed = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 900, Speed = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void Very_Small_Weight_Test()
        {
            var database = CreateDatabase();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Squats", Sets = new[] {new Set {Weight = 0.9M, Repetitions = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).Result.ToList();

            Assert.That(achievements, Is.Empty);
        }
    }
}