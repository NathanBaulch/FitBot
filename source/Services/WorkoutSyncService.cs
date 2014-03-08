using System;
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

        public async Task Run()
        {
            foreach (var user in await _database.GetUsers())
            {
                var offset = 0;
                var toDate = DateTime.MaxValue;
                while (true)
                {
                    var freshWorkouts = _fitocracy.GetWorkouts(user.Id, offset).Result;
                    if (freshWorkouts.Count == 0)
                    {
                        break;
                    }

                    var lastWorkout = freshWorkouts[freshWorkouts.Count - 1];
                    var fromDate = lastWorkout.Date;
                    var staleWorkouts = (await _database.GetWorkouts(user.Id, fromDate, toDate)).ToDictionary(workout => workout.Id);
                    var finished = false;
                    foreach (var freshWorkout in freshWorkouts)
                    {
                        freshWorkout.Hash = ComputeWorkoutHashCode(freshWorkout);
                        Workout staleWorkout;
                        if (!staleWorkouts.TryGetValue(freshWorkout.Id, out staleWorkout))
                        {
                            _database.Insert(freshWorkout);
                        }
                        else
                        {
                            if (staleWorkout.Hash != freshWorkout.Hash)
                            {
                                _database.Update(freshWorkout);
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
                    }

                    if (finished)
                    {
                        break;
                    }

                    offset += freshWorkouts.Count;
                    toDate = fromDate;
                }
            }
        }

        private static int ComputeWorkoutHashCode(Workout workout)
        {
            unchecked
            {
                var hash = workout.Points;
                foreach (var activity in workout.Activities)
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
                        hash = (hash*397) ^ set.IsPb.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}