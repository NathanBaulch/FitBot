using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Achievements;

// TODO: highest speed for distance or greater (cardio)
// TODO: highest reps for weight or greater (weights)
// TODO: lifetime distance increments (cardio)
// TODO: lifetime rep increments (weights)
// TODO: lifetime time increments (sports)
// TODO: daily distance (cardio)
// TODO: daily reps (weights)
// TODO: daily time (sports)
// TODO: 1 year greatest distance (cardio)
// TODO: 1 year highest weight (weights)
// TODO: 1 year longest time (sports)

namespace FitBot.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;
        private readonly IList<IAchievementProvider> _achievements;

        public AchievementService(IDatabaseService database, IFitocracyService fitocracy, IEnumerable<IAchievementProvider> achievements)
        {
            _database = database;
            _fitocracy = fitocracy;
            _achievements = achievements.ToList();
        }

        public async Task Execute()
        {
            foreach (var user in await _database.GetUsersWithDirtyDate())
            {
                foreach (var workout in await _database.GetWorkouts(user.Id, user.DirtyDate.Value, DateTime.MaxValue))
                {
                    if (user.DirtyDate != workout.Date)
                    {
                        user.DirtyDate = workout.Date;
                        _database.Update(user);
                    }

                    var text = string.Join(
                        ", ",
                        (await Task.WhenAll(_achievements.Select(achievement => achievement.Execute(workout))))
                            .SelectMany(items => items)
                            .OrderBy(comment => comment));
                    if (!string.IsNullOrEmpty(text))
                    {
                        var hash = text.GetHashCode();
                        if (workout.CommentId != null)
                        {
                            if (workout.CommentHash == hash)
                            {
                                continue;
                            }
                            await _fitocracy.DeleteComment(workout.CommentId.Value);
                        }
                        await _fitocracy.PostComment(workout.Id, text);
                        workout.CommentHash = hash;
                        _database.Update(workout);
                    }
                    else if (workout.CommentId != null)
                    {
                        await _fitocracy.DeleteComment(workout.CommentId.Value);
                        workout.CommentHash = null;
                        _database.Update(workout);
                    }
                }

                if (user.DirtyDate != null)
                {
                    user.DirtyDate = null;
                    _database.Update(user);
                }
            }
        }
    }
}