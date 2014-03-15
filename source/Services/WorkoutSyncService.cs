using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class WorkoutSyncService : IWorkoutSyncService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;

        public WorkoutSyncService(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task Execute()
        {
            foreach (var user in await _database.GetUsers())
            {
                var isNewUser = (await _database.GetWorkoutCount(user.Id)) == 0;
                var offset = 0;
                var toDate = DateTime.MaxValue;
                DateTime? dirtyDate = null;
                var processedIds = new HashSet<long>();
                while (true)
                {
                    var freshWorkouts = _fitocracy.GetWorkouts(user.Id, offset).Result;
                    if (freshWorkouts.Count == 0)
                    {
                        if (!isNewUser)
                        {
                            _database.DeleteWorkoutsBefore(toDate);
                        }
                        break;
                    }

                    offset += freshWorkouts.Count;

                    freshWorkouts = freshWorkouts.Where(workout => !processedIds.Contains(workout.Id)).ToList();
                    if (freshWorkouts.Count == 0)
                    {
                        continue;
                    }
                    foreach (var freshWorkout in freshWorkouts)
                    {
                        processedIds.Add(freshWorkout.Id);
                    }

                    if (isNewUser)
                    {
                        foreach (var freshWorkout in freshWorkouts)
                        {
                            freshWorkout.SyncDate = DateTime.UtcNow;
                            freshWorkout.ActivitiesHash = ComputeActivitiesHashCode(freshWorkout.Activities);
                            _database.Insert(freshWorkout, true);
                        }
                    }
                    else
                    {
                        var lastWorkout = freshWorkouts[freshWorkouts.Count - 1];
                        var fromDate = lastWorkout.Date;
                        var staleWorkouts = (await _database.GetWorkouts(user.Id, fromDate, toDate)).ToDictionary(workout => workout.Id);
                        var finished = false;
                        foreach (var freshWorkout in freshWorkouts)
                        {
                            freshWorkout.SyncDate = DateTime.UtcNow;
                            freshWorkout.ActivitiesHash = ComputeActivitiesHashCode(freshWorkout.Activities);
                            Workout staleWorkout;
                            if (!staleWorkouts.TryGetValue(freshWorkout.Id, out staleWorkout))
                            {
                                _database.Insert(freshWorkout, true);
                                dirtyDate = freshWorkout.Date;
                            }
                            else
                            {
                                if (staleWorkout.Date != freshWorkout.Date ||
                                    staleWorkout.Points != freshWorkout.Points ||
                                    staleWorkout.CommentId != freshWorkout.CommentId ||
                                    staleWorkout.IsPropped != freshWorkout.IsPropped ||
                                    staleWorkout.ActivitiesHash != freshWorkout.ActivitiesHash)
                                {
                                    _database.Update(freshWorkout, staleWorkout.ActivitiesHash != freshWorkout.ActivitiesHash);
                                    dirtyDate = freshWorkout.Date;
                                }
                                else if (freshWorkout == lastWorkout)
                                {
                                    finished = true;
                                }
                                staleWorkouts.Remove(freshWorkout.Id);
                            }
                        }

                        foreach (var staleWorkout in staleWorkouts.Values)
                        {
                            _database.Delete(staleWorkout);
                            if (dirtyDate > staleWorkout.Date)
                            {
                                dirtyDate = staleWorkout.Date;
                            }
                        }

                        if (finished)
                        {
                            break;
                        }

                        toDate = fromDate;
                    }
                }

                if (user.DirtyDate != dirtyDate)
                {
                    user.DirtyDate = dirtyDate;
                    _database.Update(user);
                }
            }
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
                        hash = (hash*397) ^ set.Incline.GetHashCode();
                        hash = (hash*397) ^ (set.Difficulty ?? string.Empty).GetHashCode();
                        hash = (hash*397) ^ set.IsPr.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}