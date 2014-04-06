using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class WorkoutPullService : IWorkoutPullService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;

        public WorkoutPullService(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task<IEnumerable<Workout>> Pull(User user)
        {
            var changes = Enumerable.Repeat(new {operation = default(Operation), workout = default(Workout)}, 0).ToList();

            var offset = 0;
            var processedIds = new HashSet<long>();
            var toDate = DateTime.MaxValue;
            while (true)
            {
                var freshWorkouts = await _fitocracy.GetWorkouts(user.Id, offset);
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

                foreach (var workout in freshWorkouts)
                {
                    workout.ActivitiesHash = ComputeActivitiesHashCode(workout.Activities);
                }

                var fromDate = freshWorkouts.Min(workout => workout.Date);
                var staleWorkouts = (await _database.GetWorkouts(user.Id, fromDate, toDate)).ToList();
                toDate = fromDate;

                var freshLookup = freshWorkouts.ToLookup(workout => workout.Id);
                var staleLookup = staleWorkouts.ToLookup(workout => workout.Id);
                var newChanges = freshWorkouts
                    .Select(workout => workout.Id)
                    .Union(staleWorkouts.Select(workout => workout.Id))
                    .Select(id => new {stale = staleLookup[id].FirstOrDefault(), fresh = freshLookup[id].FirstOrDefault()})
                    .OrderBy(pair => (pair.fresh ?? pair.stale).Date)
                    .Select(match => new
                        {
                            operation = match.fresh != null
                                            ? match.stale != null
                                                  ? match.fresh.HasChanges(match.stale)
                                                        ? match.stale.ActivitiesHash != match.fresh.ActivitiesHash
                                                              ? Operation.UpdateDeep
                                                              : Operation.UpdateShallow
                                                        : Operation.None
                                                  : Operation.Insert
                                            : Operation.Delete,
                            workout = match.fresh ?? match.stale
                        })
                    .ToList();

                if (newChanges[0].operation == Operation.None)
                {
                    changes.AddRange(newChanges.SkipWhile(item => item.operation == Operation.None).Reverse());
                    break;
                }

                newChanges.Reverse();
                changes.AddRange(newChanges);
            }

            changes.Reverse();

            foreach (var item in changes)
            {
                switch (item.operation)
                {
                    case Operation.Insert:
                        _database.Insert(item.workout);
                        break;
                    case Operation.UpdateDeep:
                    case Operation.UpdateShallow:
                        _database.Update(item.workout, item.operation == Operation.UpdateDeep);
                        break;
                    case Operation.Delete:
                        _database.Delete(item.workout);
                        break;
                }
            }

            return changes.Where(item => item.workout.Date > user.InsertDate && item.operation != Operation.Delete)
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

        private enum Operation
        {
            None,
            Insert,
            UpdateDeep,
            UpdateShallow,
            Delete
        }
    }
}