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
            var workouts = new List<Workout>();

            var offset = 0;
            var processedIds = new HashSet<long>();
            var toDate = DateTime.MaxValue;
            while (true)
            {
                var freshWorkouts = await _fitocracy.GetWorkouts(user.Id, offset);
                if (freshWorkouts.Count == 0)
                {
                    if (!user.IsNew)
                    {
                        _database.DeleteWorkoutsBefore(user.Id, toDate);
                    }
                    break;
                }

                offset += freshWorkouts.Count;

                freshWorkouts = freshWorkouts.Where(workout => processedIds.Add(workout.Id)).ToList();
                if (freshWorkouts.Count == 0)
                {
                    continue;
                }

                if (user.IsNew)
                {
                    foreach (var freshWorkout in freshWorkouts)
                    {
                        freshWorkout.PullDate = DateTime.UtcNow;
                        freshWorkout.ActivitiesHash = ComputeActivitiesHashCode(freshWorkout.Activities);
                        _database.Insert(freshWorkout);
                    }
                }
                else
                {
                    var fromDate = freshWorkouts.Min(workout => workout.Date);
                    var staleWorkouts = (await _database.GetWorkouts(user.Id, fromDate, toDate)).ToDictionary(workout => workout.Id);
                    var index = 0;
                    var dirtyIndex = 0;
                    foreach (var freshWorkout in freshWorkouts)
                    {
                        index++;
                        freshWorkout.PullDate = DateTime.UtcNow;
                        freshWorkout.ActivitiesHash = ComputeActivitiesHashCode(freshWorkout.Activities);
                        Workout staleWorkout;
                        if (!staleWorkouts.TryGetValue(freshWorkout.Id, out staleWorkout))
                        {
                            _database.Insert(freshWorkout);
                            dirtyIndex = index;
                        }
                        else
                        {
                            if (freshWorkout.HasChanges(staleWorkout))
                            {
                                _database.Update(freshWorkout, staleWorkout.ActivitiesHash != freshWorkout.ActivitiesHash);
                                dirtyIndex = index;
                            }
                            staleWorkouts.Remove(freshWorkout.Id);
                        }
                    }

                    if (dirtyIndex > 0)
                    {
                        workouts.AddRange(freshWorkouts.Take(dirtyIndex));
                    }

                    foreach (var staleWorkout in staleWorkouts.Values)
                    {
                        _database.Delete(staleWorkout);
                    }

                    if (dirtyIndex < freshWorkouts.Count)
                    {
                        break;
                    }

                    toDate = fromDate;
                }
            }

            workouts.Reverse();
            return workouts;
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