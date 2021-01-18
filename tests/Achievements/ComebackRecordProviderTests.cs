using System;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Model;
using FitBot.Services;
using Moq;
using NUnit.Framework;

namespace FitBot.Test.Achievements
{
    [TestFixture]
    public class ComebackRecordProviderTests
    {
        [Test]
        public async Task Normal_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 3000}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Cycling"));
            Assert.That(achievement.Distance, Is.EqualTo(2000M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Cycling comeback record: 2 km"));
        }

        [Test]
        public async Task New_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 2000}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 3000}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Equal_To_Old_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 2000}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Cycling"));
            Assert.That(achievement.Distance, Is.EqualTo(2000M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Cycling comeback record: 2 km"));
        }

        [Test]
        public async Task Equal_To_Recent_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 2000}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 1000}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Multiple_Same_Activities_With_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Cycling", Sets = new[] {new Set {Distance = 3000}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Cycling")).Returns(ActivityCategory.Cardio);

            var workout = new Workout
                {
                    Date = new DateTime(2015, 1, 1),
                    Activities = new[]
                        {
                            new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000}}},
                            new Activity {Name = "Cycling", Group = "Cycling", Sets = new[] {new Set {Distance = 2000}}}
                        }
                };

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Cycling"));
            Assert.That(achievement.Distance, Is.EqualTo(2000M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Cycling comeback record: 2 km"));
        }

        [Test]
        public async Task Normal_Weights_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 3}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Squats comeback record: 2 kg"));
        }

        [Test]
        public async Task New_Weights_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 3}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Equal_To_Old_Weights_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Squats comeback record: 2 kg"));
        }

        [Test]
        public async Task Equal_To_Recent_Weights_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 1}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Equal_To_Recent_Weights_Record_With_More_Reps_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 3, Repetitions = 1}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 2}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 3}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Squats"));
            Assert.That(achievement.Weight, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Squats comeback record: 2 kg for 3 reps"));
        }

        [Test]
        public async Task Equal_To_Recent_Weights_Record_With_Less_Reps_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 3, Repetitions = 1}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 2}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Weight = 2, Repetitions = 1}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Normal_Reps_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 3}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Repetitions = 2}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Squats"));
            Assert.That(achievement.Repetitions, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Squats comeback record: 2 reps"));
        }

        [Test]
        public async Task New_Reps_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Repetitions = 3}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }

        [Test]
        public async Task Equal_To_Old_Reps_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Repetitions = 2}}}}};

            var achievements = (await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout)).ToList();

            Assert.That(achievements.Count, Is.EqualTo(1));
            var achievement = achievements[0];
            Assert.That(achievement.Type, Is.EqualTo("ComebackRecord"));
            Assert.That(achievement.Activity, Is.EqualTo("Squats"));
            Assert.That(achievement.Repetitions, Is.EqualTo(2M));
            Assert.That(achievement.CommentText, Is.EqualTo("1 year Squats comeback record: 2 reps"));
        }

        [Test]
        public async Task Equal_To_Recent_Reps_Record_Test()
        {
            var database = new SQLiteDatabaseService();
            database.Insert(new Workout {Id = 0, Date = new DateTime(2013, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 2}}}}});
            database.Insert(new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = new[] {new Activity {Name = "Squats", Sets = new[] {new Set {Repetitions = 1}}}}});

            var activityGrouping = new Mock<IActivityGroupingService>();
            activityGrouping.Setup(x => x.GetGroupCategory("Squats")).Returns(ActivityCategory.Weights);

            var workout = new Workout {Date = new DateTime(2015, 1, 1), Activities = new[] {new Activity {Name = "Squats", Group = "Squats", Sets = new[] {new Set {Repetitions = 1}}}}};

            var achievements = await new ComebackRecordProvider(database, activityGrouping.Object).Execute(workout);

            Assert.That(achievements, Is.Empty);
        }
    }
}