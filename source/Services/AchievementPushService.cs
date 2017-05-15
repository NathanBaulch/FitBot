using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class AchievementPushService : IAchievementPushService
    {
        private readonly IDatabaseService _database;
        private readonly IFitocracyService _fitocracy;

        public AchievementPushService(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task Push(IEnumerable<Achievement> achievements, CancellationToken cancel = default(CancellationToken))
        {
            foreach (var group in achievements.GroupBy(achievement => achievement.WorkoutId))
            {
                foreach (var achievement in group)
                {
                    if (achievement.IsPropped)
                    {
                        await _fitocracy.GiveProp(achievement.WorkoutId);
                    }

                    if (achievement.CommentId != null)
                    {
                        await _fitocracy.DeleteComment(achievement.CommentId.Value);
                    }

                    if (achievement.CommentText != null)
                    {
                        await _fitocracy.AddComment(achievement.WorkoutId, achievement.CommentText);
                    }
                }

                var commentAchievements = group.Where(achievement => achievement.CommentText != null).ToList();
                if (commentAchievements.Count > 0)
                {
                    var comments = await _fitocracy.GetWorkoutComments(group.Key);

                    foreach (var achievement in commentAchievements)
                    {
                        var matchingComments = comments.Where(comment => comment.Value == achievement.CommentText).ToList();
                        if (matchingComments.Count == 0)
                        {
                            throw new ApplicationException($"Comment '{achievement.CommentText}' not found on workout {achievement.WorkoutId}");
                        }
                        if (matchingComments.Count > 1)
                        {
                            throw new ApplicationException($"Duplicate comment '{achievement.CommentText}' on workout {achievement.WorkoutId}");
                        }
                        achievement.CommentId = matchingComments[0].Key;
                        _database.Update(achievement);
                    }
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}