using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        public async Task<IEnumerable<Achievement>> Process(User user, IEnumerable<Workout> workouts, CancellationToken cancel = default(CancellationToken))
        {
            var achievements = new List<Achievement>();

            foreach (var workout in workouts)
            {
                var latestAchievements = await (workout.State == WorkoutState.Unresolved
                                                    ? _database.GetAchievements(workout.Id)
                                                    : ProcessAchievements(workout));
                latestAchievements = ProcessComments(workout, latestAchievements).ToList();

                if (workout.Date > user.InsertDate.AddDays(-7) && workout.Date > DateTime.UtcNow.AddDays(-30))
                {
                    achievements.AddRange(latestAchievements);
                }

                cancel.ThrowIfCancellationRequested();
            }

            return achievements;
        }

        private async Task<IEnumerable<Achievement>> ProcessAchievements(Workout workout)
        {
            var staleAchievements = (await _database.GetAchievements(workout.Id)).ToList();
            var tasks = _providers.Select(achievement => achievement.Execute(workout)).ToList();
            await Task.WhenAll(tasks);
            var freshAchievements = tasks.SelectMany(task => task.Result).ToList();

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

                    staleAchievements.Remove(staleAchievement);
                }
            }

            foreach (var staleAchievement in staleAchievements)
            {
                _database.Delete(staleAchievement);
            }

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
                        Trace.TraceInformation($"Comment {achievement.CommentId} missing on workout {workout.Id}");
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