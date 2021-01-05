using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;
using Moq;
using NUnit.Framework;

namespace FitBot.Test.Services
{
    [TestFixture]
    public class WorkoutPullServiceTests
    {
        [Test]
        public void Empty_Test()
        {
            var database = new Mock<IDatabaseService>();

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.Empty);
        }

        [Test]
        public void Single_Unchanged_Test()
        {
            var workout = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};

            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout}));

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.Empty);
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Single_Insert_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout}));
            database.Verify(x => x.Insert(workout));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Single_Update_Shallow_Test()
        {
            var database = new Mock<IDatabaseService>();
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Points = 1, Activities = Array.Empty<Activity>()};
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout1}));

            var fitocracy = new Mock<IFitocracyService>();
            var workout1A = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Points = 2, Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1A}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1A}));
            database.Verify(x => x.Update(workout1A, false));
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Single_Update_Deep_Test()
        {
            var database = new Mock<IDatabaseService>();
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>(), ActivitiesHash = 1};
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout1}));

            var fitocracy = new Mock<IFitocracyService>();
            var workout1A = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>(), ActivitiesHash = 2};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1A}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1A}));
            database.Verify(x => x.Update(workout1A, true));
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Single_Delete_Test()
        {
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};

            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout2, workout1}));

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.Empty);
            database.Verify(x => x.Delete(workout2));
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
        }

        [Test]
        public void Delete_Old_Test()
        {
            var database = new Mock<IDatabaseService>();

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.Empty);
            database.Verify(x => x.DeleteWorkouts(0, DateTime.MaxValue));
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Multiple_Insert_Single_Page_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2, workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 2)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1, workout2}));
            database.Verify(x => x.Insert(workout1));
            database.Verify(x => x.Insert(workout2));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Multiple_Insert_Multiple_Page_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 2)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1, workout2}));
            database.Verify(x => x.Insert(workout1));
            database.Verify(x => x.Insert(workout2));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Duplicate_Within_Page_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout, workout}));
            fitocracy.Setup(x => x.GetWorkouts(0, 2)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout}));
            database.Verify(x => x.Insert(workout));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Duplicate_Across_Pages_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2, workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 3)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1, workout2}));
            database.Verify(x => x.Insert(workout1));
            database.Verify(x => x.Insert(workout2));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Full_Duplicate_Page_Test()
        {
            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2, workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 2)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2, workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 4)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1, workout2}));
            database.Verify(x => x.Insert(workout1));
            database.Verify(x => x.Insert(workout2));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Half_Unchanged_Page_Test()
        {
            var workout4 = new Workout {Id = 4, Date = new DateTime(2014, 1, 4), Activities = Array.Empty<Activity>()};
            var workout3 = new Workout {Id = 3, Date = new DateTime(2014, 1, 3), Activities = Array.Empty<Activity>()};
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};

            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout1, workout2}));

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout4, workout3, workout2, workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 4)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout3, workout4}));
            database.Verify(x => x.Insert(workout3));
            database.Verify(x => x.Insert(workout4));
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Initial_Changed_Test()
        {
            var database = new Mock<IDatabaseService>();
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Points = 1, Activities = Array.Empty<Activity>()};
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout1}));

            var fitocracy = new Mock<IFitocracyService>();
            var workout1A = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Points = 2, Activities = Array.Empty<Activity>()};
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1A}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.EqualTo(new[] {workout1A}));
            database.Verify(x => x.Update(workout1A, false));
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }

        [Test]
        public void Dates_Out_Of_Sequence_Across_Pages_Test()
        {
            var workout2 = new Workout {Id = 2, Date = new DateTime(2014, 1, 2), Activities = Array.Empty<Activity>()};
            var workout1 = new Workout {Id = 1, Date = new DateTime(2014, 1, 1), Activities = Array.Empty<Activity>()};

            var database = new Mock<IDatabaseService>();
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 1), DateTime.MaxValue)).Returns(Task.FromResult<IEnumerable<Workout>>(new[] {workout2, workout1}));
            database.Setup(x => x.GetWorkouts(0, new DateTime(2014, 1, 2), new DateTime(2014, 1, 1))).Returns(Task.FromResult<IEnumerable<Workout>>(Array.Empty<Workout>()));

            var fitocracy = new Mock<IFitocracyService>();
            fitocracy.Setup(x => x.GetWorkouts(0, 0)).Returns(Task.FromResult<IList<Workout>>(new[] {workout1}));
            fitocracy.Setup(x => x.GetWorkouts(0, 1)).Returns(Task.FromResult<IList<Workout>>(new[] {workout2}));
            fitocracy.Setup(x => x.GetWorkouts(0, 2)).Returns(Task.FromResult<IList<Workout>>(Array.Empty<Workout>()));

            var activityGrouping = new Mock<IActivityGroupingService>();

            var workouts = new WorkoutPullService(database.Object, fitocracy.Object, activityGrouping.Object).Pull(new User {InsertDate = DateTime.Now}).Result;

            Assert.That(workouts, Is.Empty);
            database.Verify(x => x.Insert(It.IsAny<Workout>()), Times.Never);
            database.Verify(x => x.Update(It.IsAny<Workout>(), It.IsAny<bool>()), Times.Never);
            database.Verify(x => x.Delete(It.IsAny<Workout>()), Times.Never);
        }
    }
}