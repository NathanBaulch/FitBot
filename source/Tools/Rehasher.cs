using System;
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

        public Rehasher(IDatabaseService database, IActivityHashService hasher)
        {
            _database = database;
            _hasher = hasher;
        }

        public async Task Run(bool gentle, bool dryRun, CancellationToken cancel)
        {
            var users = (await _database.GetUsers()).ToList();
            var i = 0;
            var updated = 0;
            var total = 0;
            using var progress = new ProgressBar(users.Count, "", new ProgressBarOptions {ForegroundColor = ConsoleColor.DarkGreen, ShowEstimatedDuration = true});
            await using (cancel.Register(() => progress.Message = $"Rehashing aborted - updated {updated:n0} of {total:n0} workout(s)" + (dryRun ? " (dry-run)" : "")))
            {
                var start = DateTime.Now;
                Parallel.ForEach(users, new ParallelOptions {CancellationToken = cancel, MaxDegreeOfParallelism = gentle ? 1 : -1}, async user =>
                {
                    progress.Message = "Rehashing workouts for user " + user.Username + (dryRun ? " (dry-run)" : "");
                    foreach (var workout in await _database.GetWorkouts(user.Id))
                    {
                        var hash = _hasher.Hash(await _database.GetActivities(workout.Id));
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
                    }
                    progress.Tick();
                    progress.EstimatedDuration = (DateTime.Now - start) * users.Count / Interlocked.Increment(ref i);
                });
                progress.Message = $"Rehashing complete - updated {updated:n0} of {total:n0} workout(s)" + (dryRun ? " (dry-run)" : "");
            }
        }
    }
}