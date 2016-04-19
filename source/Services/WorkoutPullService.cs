using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class WorkoutPullService : IWorkoutPullService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;
        private readonly IActivityGroupingService _grouping;

        public WorkoutPullService(IDatabaseService database, IFitocracyService fitocracy, IActivityGroupingService grouping)
        {
            _database = database;
            _fitocracy = fitocracy;
            _grouping = grouping;
        }

        public async Task<IEnumerable<Workout>> Pull(User user, CancellationToken cancel = default(CancellationToken))
        {
            var changes = Enumerable.Repeat(new {workout = default(Workout), operation = default(byte)}, 0).ToList();
            const byte none = 0;
            const byte insert = 1;
            const byte updateDeep = 2;
            const byte updateShallow = 3;
            const byte delete = 4;

            var offset = 0;
            var processedIds = new HashSet<long>();
            var toDate = DateTime.MaxValue;
            var deletedWorkouts = new List<Workout>();
            while (true)
            {
                var freshWorkouts = await _fitocracy.GetWorkouts(user.Id, offset);
                var freshLookup = freshWorkouts.ToLookup(workout => workout.Id);

                foreach (var workout in deletedWorkouts.Where(workout => !freshLookup[workout.Id].Any()))
                {
                    changes.Add(new {workout, operation = delete});
                }

                if (freshWorkouts.Count == 0)
                {
                    _database.DeleteWorkoutsBefore(user.Id, toDate);
                    break;
                }
                offset += freshWorkouts.Count;

                freshWorkouts = freshWorkouts.Where(workout => processedIds.Add(workout.Id)).ToList();
                if (freshWorkouts.Count == 0)
                {
                    continue;
                }

                var fromDate = freshWorkouts.Min(workout => workout.Date);
                var staleWorkouts = (await _database.GetWorkouts(user.Id, fromDate, toDate)).ToList();
                toDate = fromDate;

                var staleLookup = staleWorkouts.ToLookup(workout => workout.Id);
                changes.AddRange(freshWorkouts
                                     .Select(workout =>
                                         {
                                             workout.ActivitiesHash = ComputeActivitiesHashCode(workout.Activities);
                                             foreach (var activity in workout.Activities)
                                             {
                                                 activity.Group = _grouping.GetActvityGroup(activity.Name);
                                             }
                                             var staleWorkout = staleLookup[workout.Id].FirstOrDefault() ?? deletedWorkouts.FirstOrDefault(item => item.Id == workout.Id);
                                             return new
                                                 {
                                                     workout,
                                                     operation = staleWorkout != null
                                                                     ? workout.HasChanges(staleWorkout)
                                                                           ? staleWorkout.ActivitiesHash != workout.ActivitiesHash
                                                                                 ? updateDeep
                                                                                 : updateShallow
                                                                           : none
                                                                     : insert
                                                 };
                                         }));
                deletedWorkouts = staleWorkouts.Where(workout => !freshLookup[workout.Id].Any()).ToList();

                if (deletedWorkouts.Count == 0 && changes[changes.Count - 1].operation == none)
                {
                    break;
                }

                cancel.ThrowIfCancellationRequested();
            }

            changes.Reverse();
            changes.Sort((left, right) => left.workout.Date.CompareTo(right.workout.Date));

            foreach (var item in changes)
            {
                switch (item.operation)
                {
                    case insert:
                        _database.Insert(item.workout);
                        break;
                    case updateDeep:
                    case updateShallow:
                        _database.Update(item.workout, item.operation == updateDeep);
                        break;
                    case delete:
                        _database.Delete(item.workout);
                        break;
                }
            }

            return changes.Where(item => item.operation != delete)
                          .SkipWhile(item => item.operation == none)
                          .Select(item => item.workout)
                          .ToList();
        }

        private static int ComputeActivitiesHashCode(IEnumerable<Activity> activities)
        {
            unchecked
            {
                var hash = 0;
                foreach (var activity in activities)
                {
                    hash = (hash*397) ^ activity.Name.GetHashCode();
                    foreach (var set in activity.Sets)
                    {
                        hash = (hash*397) ^ set.Points;
                        hash = (hash*397) ^ set.Distance.GetHashCode();
                        hash = (hash*397) ^ set.Duration.GetHashCode();
                        hash = (hash*397) ^ set.Speed.GetHashCode();
                        hash = (hash*397) ^ set.Repetitions.GetHashCode();
                        hash = (hash*397) ^ set.Weight.GetHashCode();
                        hash = (hash*397) ^ set.HeartRate.GetHashCode();
                        hash = (hash*397) ^ (set.Difficulty ?? string.Empty).GetHashCode();
                        hash = (hash*397) ^ set.IsPr.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}