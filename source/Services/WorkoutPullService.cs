using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public IEnumerable<Workout> Pull(User user, CancellationToken cancel = default)
        {
            var workouts = new List<Workout>();

            var offset = 0;
            var processedIds = new HashSet<long>();
            var toDate = DateTime.MaxValue;
            var deletedWorkouts = new List<Workout>();
            while (true)
            {
                var freshWorkouts = _fitocracy.GetWorkouts(user.Id, offset);
                var freshLookup = freshWorkouts.ToLookup(workout => workout.Id);

                foreach (var workout in deletedWorkouts.Where(workout => !freshLookup[workout.Id].Any()))
                {
                    workout.State = WorkoutState.Deleted;
                    workouts.Add(workout);
                }

                if (freshWorkouts.Count == 0)
                {
                    _database.DeleteWorkouts(user.Id, toDate);
                    break;
                }
                offset += freshWorkouts.Count;

                freshWorkouts = freshWorkouts.Where(workout => processedIds.Add(workout.Id)).ToList();
                if (freshWorkouts.Count == 0)
                {
                    continue;
                }

                var fromDate = freshWorkouts.Min(workout => workout.Date);
                var staleWorkouts = _database.GetWorkouts(user.Id, fromDate, toDate).ToList();
                var staleLookup = staleWorkouts.ToLookup(workout => workout.Id);
                toDate = fromDate;

                foreach (var workout in freshWorkouts)
                {
                    workout.ActivitiesHash = ComputeActivitiesHashCode(workout.Activities);
                    foreach (var activity in workout.Activities)
                    {
                        activity.Group = _grouping.GetActivityGroup(activity.Name);
                    }

                    var staleWorkout = staleLookup[workout.Id].FirstOrDefault() ?? deletedWorkouts.FirstOrDefault(item => item.Id == workout.Id);
                    workout.State = staleWorkout != null
                        ? workout.HasChanges(staleWorkout)
                            ? staleWorkout.ActivitiesHash != workout.ActivitiesHash
                                ? WorkoutState.UpdatedDeep
                                : WorkoutState.Updated
                            : WorkoutState.Unchanged
                        : WorkoutState.Added;
                    workouts.Add(workout);
                }

                deletedWorkouts = staleWorkouts.Where(workout => !freshLookup[workout.Id].Any()).ToList();
                if (deletedWorkouts.Count == 0 && workouts[^1].State == WorkoutState.Unchanged)
                {
                    break;
                }

                cancel.ThrowIfCancellationRequested();
            }

            workouts.Reverse();
            var unresolvedIds = _database.GetUnresolvedWorkoutIds(user.Id, user.InsertDate.AddDays(-7)).ToList();

            foreach (var workout in workouts.ToList())
            {
                switch (workout.State)
                {
                    case WorkoutState.Added:
                        _database.Insert(workout);
                        break;
                    case WorkoutState.UpdatedDeep:
                        _database.Update(workout, true);
                        unresolvedIds.Remove(workout.Id);
                        break;
                    case WorkoutState.Updated:
                        _database.Update(workout);
                        unresolvedIds.Remove(workout.Id);
                        break;
                    case WorkoutState.Deleted:
                        _database.Delete(workout);
                        workouts.Remove(workout);
                        unresolvedIds.Remove(workout.Id);
                        break;
                    case WorkoutState.Unchanged:
                        if (unresolvedIds.Remove(workout.Id))
                        {
                            workout.State = WorkoutState.Unresolved;
                        }
                        break;
                }
            }

            while (workouts.Count > 0 && workouts[0].State == WorkoutState.Unchanged)
            {
                workouts.RemoveAt(0);
            }

            foreach (var workout in unresolvedIds.Select(_fitocracy.GetWorkout))
            {
                workout.State = WorkoutState.Unresolved;
                workouts.Add(workout);
            }

            return workouts.OrderBy(workout => workout.Date).ToList();
        }

        private static int ComputeActivitiesHashCode(IEnumerable<Activity> activities)
        {
            unchecked
            {
                var hash = 0;
                foreach (var activity in activities)
                {
                    hash = (hash * 397) ^ activity.Name.GetHashCode();
                    foreach (var set in activity.Sets)
                    {
                        hash = (hash * 397) ^ set.Points;
                        hash = (hash * 397) ^ set.Distance.GetHashCode();
                        hash = (hash * 397) ^ set.Duration.GetHashCode();
                        hash = (hash * 397) ^ set.Speed.GetHashCode();
                        hash = (hash * 397) ^ set.Repetitions.GetHashCode();
                        hash = (hash * 397) ^ set.Weight.GetHashCode();
                        hash = (hash * 397) ^ set.HeartRate.GetHashCode();
                        hash = (hash * 397) ^ (set.Difficulty ?? string.Empty).GetHashCode();
                        hash = (hash * 397) ^ set.IsPr.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}