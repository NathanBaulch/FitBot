using System;
using System.Collections.Generic;
using System.Linq;
using FitBot.Achievements;
using FitBot.Model;
using Microsoft.Extensions.Logging;

namespace FitBot.Services
{
    public class AchievementService : IAchievementService
    {
        private readonly IDatabaseService _database;
        private readonly IList<IAchievementProvider> _providers;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(IDatabaseService database, IEnumerable<IAchievementProvider> providers, ILogger<AchievementService> logger)
        {
            _database = database;
            _providers = providers.ToList();
            _logger = logger;
        }

        public IEnumerable<Achievement> Process(User user, IEnumerable<Workout> workouts)
        {
            var achievements = new List<Achievement>();
            var cutoff = new DateTime(Math.Max(user.InsertDate.AddDays(-7).Ticks, DateTime.UtcNow.AddDays(-30).Ticks));
            Process(workouts, cutoff, achievements);
            Process(_database.GetUnprocessedWorkouts(user.Id), cutoff, achievements);
            return achievements;
        }

        private void Process(IEnumerable<Workout> workouts, DateTime cutoff, List<Achievement> achievements)
        {
            foreach (var workout in workouts)
            {
                var latestAchievements = ProcessComments(workout, ProcessAchievements(workout));
                if (workout.Date > cutoff)
                {
                    achievements.AddRange(latestAchievements);
                }
            }
        }

        private IEnumerable<Achievement> ProcessAchievements(Workout workout)
        {
            var staleAchievements = _database.GetAchievements(workout.Id).ToList();
            if (workout.IsProcessed)
            {
                return staleAchievements;
            }

            var freshAchievements = _providers.SelectMany(achievement => achievement.Execute(workout)).ToList();

            foreach (var achievement in freshAchievements)
            {
                achievement.WorkoutId = workout.Id;

                var matchingAchievements = staleAchievements
                    .Where(item => item.Type == achievement.Type &&
                                   item.Group == achievement.Group &&
                                   item.Activity == achievement.Activity)
                    .ToList();
                if (matchingAchievements.Count == 0)
                {
                    _database.Insert(achievement);
                }
                else
                {
                    var staleAchievement = matchingAchievements.FirstOrDefault(item => !item.HasChanges(achievement));
                    if (staleAchievement == null)
                    {
                        staleAchievement = matchingAchievements[0];
                        achievement.Id = staleAchievement.Id;
                        _database.Update(achievement);
                    }
                    else
                    {
                        achievement.Id = staleAchievement.Id;
                    }

                    staleAchievements.Remove(staleAchievement);
                }
            }

            foreach (var staleAchievement in staleAchievements)
            {
                _database.Delete(staleAchievement);
            }

            _database.UpdateIsProcessed(workout.Id, true);
            return freshAchievements;
        }

        private IEnumerable<Achievement> ProcessComments(Workout workout, IEnumerable<Achievement> achievements)
        {
            var staleComments = workout.Comments.ToList();

            foreach (var achievement in achievements)
            {
                if (achievement.IsPropped)
                {
                    yield return achievement;
                }
                else if (achievement.CommentId != null)
                {
                    var matchingComment = staleComments.FirstOrDefault(comment => comment.Id == achievement.CommentId);
                    if (matchingComment != null)
                    {
                        staleComments.Remove(matchingComment);
                    }
                    else
                    {
                        _logger.LogInformation("Comment {0} missing on workout {1}", achievement.CommentId, workout.Id);
                    }
                }
                else
                {
                    var matchingComment = staleComments.FirstOrDefault(comment => comment.Text == achievement.CommentText);
                    if (matchingComment != null)
                    {
                        staleComments.Remove(matchingComment);
                        achievement.CommentId = matchingComment.Id;
                        _database.UpdateCommentId(achievement.Id, matchingComment.Id);
                    }
                    else
                    {
                        yield return achievement;
                    }
                }
            }

            foreach (var staleComment in staleComments)
            {
                yield return new Achievement {CommentId = staleComment.Id};
            }
        }
    }
}