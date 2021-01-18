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
    public class QualifiedRecordProviderTests
    {
        [Test]
        public void Normal_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000, Speed = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 1000, Speed = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).ToList();

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
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000, Speed = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 900, Speed = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void Very_Small_Weight_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Squats", Sets = new[] {new Set {Weight = 0.9M, Repetitions = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void Same_As_Previous_Floating_Point_Speed_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 3200, Duration = 720}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 3200, Duration = 720}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public void Imperial_Distance_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 20000, Duration = 2000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Cycling", Sets = new[] {new Set {Distance = 16100, Duration = 1000, IsImperial = true}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("QualifiedRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Cycling"));
            Assert.That(achievement.Speed, Is.EqualTo(16.1M));
            Assert.That(achievement.Distance, Is.EqualTo(16093.44M));
            Assert.That(achievement.CommentText, Is.EqualTo("Qualified Cycling record: 36 mph for 10 mi or more"));
        }

        [Test]
        public void Imperial_Weight_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 100, Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Squats", Sets = new[] {new Set {Weight = 46, Repetitions = 2, IsImperial = true}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("QualifiedRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(45.36M));
            Assert.That(achievement.Repetitions, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("Qualified Squats record: 2 reps at 100 lb or more"));
        }

        [Test]
        public void Duplicate_Record_In_Single_Workout_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 100, Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Squats", Sets = new[] {new Set {Weight = 50, Repetitions = 2}}}, new Activity {Sequence = 1, Group = "Squats", Sets = new[] {new Set {Weight = 50, Repetitions = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("QualifiedRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(50M));
            Assert.That(achievement.Repetitions, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("Qualified Squats record: 2 reps at 50 kg or more"));
        }

        [Test]
        public void Duplicate_Record_In_Single_Activity_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 100, Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Group = "Squats", Sets = new[] {new Set {Weight = 50, Repetitions = 2}, new Set {Sequence = 1, Weight = 50, Repetitions = 2}}}}};

            var achievements = new QualifiedRecordProvider(database, activityGrouping.Object).Execute(workout).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("QualifiedRecord"));
            Assert.That(achievement.Group, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(50M));
            Assert.That(achievement.Repetitions, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("Qualified Squats record: 2 reps at 50 kg or more"));
        }
    }
}