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
        private readonly IList<IAchievementProvider> _providers;

        public AchievementService(IDatabaseService database, IEnumerable<IAchievementProvider> providers)
        {
            _database = database;
            _providers = providers.ToList();
        }

        public async Task<IEnumerable<Achievement>> Process(IEnumerable<Workout> workouts)
        {
            var achievements = new List<Achievement>();

            foreach (var workout in workouts)
            {
                var tasks = _providers.Select(achievement => achievement.Execute(workout)).ToList();
                var staleAchievements = (await _database.GetAchievements(workout.Id)).ToList();

                while (tasks.Count > 0)
                {
                    var task = await Task.WhenAny(tasks);

                    foreach (var freshAchievement in task.Result)
                    {
                        var matchingAchievements = staleAchievements
                            .Where(achievement => achievement.Type == freshAchievement.Type &&
                                                  achievement.Group == freshAchievement.Group)
                            .ToList();

                        if (matchingAchievements.Count == 0)
                        {
                            freshAchievement.WorkoutId = workout.Id;
                            _database.Insert(freshAchievement);
                            achievements.Add(freshAchievement);
                        }
                        else
                        {
                            var staleAchievement = matchingAchievements.FirstOrDefault(achievement => !freshAchievement.HasChanges(achievement));
                            if (staleAchievement == null)
                            {
                                staleAchievement = matchingAchievements[0];
                                freshAchievement.Id = staleAchievement.Id;
                                freshAchievement.WorkoutId = workout.Id;
                                _database.Update(freshAchievement);

                                if (freshAchievement.CommentText != staleAchievement.CommentText ||
                                    (freshAchievement.IsPropped && !staleAchievement.IsPropped))
                                {
                                    achievements.Add(freshAchievement);
                                }
                            }
                            staleAchievements.Remove(staleAchievement);
                        }
                    }

                    tasks.Remove(task);
                }

                foreach (var staleAchievement in staleAchievements)
                {
                    _database.Delete(staleAchievement);

                    if (staleAchievement.CommentId != null)
                    {
                        staleAchievement.CommentText = null;
                        achievements.Add(staleAchievement);
                    }
                }
            }

            return achievements;
        }
    }
}