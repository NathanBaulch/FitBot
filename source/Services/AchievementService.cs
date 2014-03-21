using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Achievements;
using FitBot.Model;

namespace FitBot.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;
        private readonly IList<IAchievementProvider> _providers;

        public AchievementService(IDatabaseService database, IFitocracyService fitocracy, IEnumerable<IAchievementProvider> providers)
        {
            _database = database;
            _fitocracy = fitocracy;
            _providers = providers.ToList();
        }

        public async Task Execute()
        {
            foreach (var user in await _database.GetUsersWithDirtyDate())
            {
                foreach (var workout in await _database.GetWorkouts(user.Id, user.DirtyDate.Value, DateTime.MaxValue, true))
                {
                    if (user.DirtyDate != workout.Date)
                    {
                        user.DirtyDate = workout.Date;
                        _database.Update(user);
                    }

                    var staleAchievements = (await _database.GetAchievements(workout.Id)).ToDictionary(achievement => new {achievement.Type, achievement.Group});
                    var freshAchievements = (await Task.WhenAll(_providers.Select(achievement => achievement.Execute(workout)))).SelectMany(items => items).ToList();
                    foreach (var freshAchievement in freshAchievements)
                    {
                        Achievement staleAchievement;
                        var key = new {freshAchievement.Type, freshAchievement.Group};
                        if (!staleAchievements.TryGetValue(key, out staleAchievement))
                        {
                            freshAchievement.WorkoutId = workout.Id;
                            _database.Insert(freshAchievement);
                        }
                        else
                        {
                            if (freshAchievement.Distance != staleAchievement.Distance ||
                                freshAchievement.Duration != staleAchievement.Duration ||
                                freshAchievement.Repetitions != staleAchievement.Repetitions ||
                                freshAchievement.Weight != staleAchievement.Weight)
                            {
                                freshAchievement.Id = staleAchievement.Id;
                                freshAchievement.WorkoutId = workout.Id;
                                _database.Update(freshAchievement);
                            }
                            staleAchievements.Remove(key);
                        }
                    }

                    foreach (var staleAchievement in staleAchievements.Values)
                    {
                        _database.Delete(staleAchievement);
                    }

                    var text = string.Join(", ", freshAchievements.Select(achievement => achievement.CommentText).OrderBy(comment => comment));

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
                        await _fitocracy.AddComment(workout.Id, text);
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