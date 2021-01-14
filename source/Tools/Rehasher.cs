using System;
using System.Diagnostics;
using System.Threading;
using FitBot.Services;
using System.Linq;
using System.Threading.Tasks;
using ShellProgressBar;

namespace FitBot.Tools
{
    public class Rehasher
    {
        private readonly IDatabaseService _database;
        private readonly IActivityHashService _hasher;

        public Rehasher(IDatabaseService database, IActivityHashService hasher) => (_database, _hasher) = (database, hasher);

        public void Run(bool gentle, bool dryRun, CancellationToken cancel)
        {
            var users = _database.GetUsers().ToList();
            var i = 0;
            var updated = 0;
            var total = 0;
            Console.WriteLine("");
            Console.WriteLine("");
            using var progress = new ProgressBar(users.Count, "", new ProgressBarOptions {ForegroundColor = ConsoleColor.DarkGreen, ShowEstimatedDuration = true});
            using (cancel.Register(() => progress.Message = $"Rehashing aborted - updated {updated:n0} of {total:n0} workout(s)" + (dryRun ? " (dry-run)" : "")))
            {
                var timer = Stopwatch.StartNew();
                Parallel.ForEach(users, new ParallelOptions {CancellationToken = cancel, MaxDegreeOfParallelism = gentle ? 1 : -1}, user =>
                {
                    progress.Message = "Rehashing workouts for user " + user.Username + (dryRun ? " (dry-run)" : "");
                    foreach (var workout in _database.GetWorkouts(user.Id))
                    {
                        var hash = _hasher.Hash(_database.GetActivities(workout.Id));
                        if (workout.ActivitiesHash != hash)
                        {
                            if (!dryRun)
                            {
                                workout.ActivitiesHash = hash;
                                _database.Update(workout);
                            }
                            Interlocked.Increment(ref updated);
                        }
                        Interlocked.Increment(ref total);
                        cancel.ThrowIfCancellationRequested();
                    }
                    progress.Tick();
                    progress.EstimatedDuration = timer.Elapsed * users.Count / Interlocked.Increment(ref i);
                });
                progress.Message = $"Rehashing complete - updated {updated:n0} of {total:n0} workout(s)" + (dryRun ? " (dry-run)" : "");
            }
        }
    }
}