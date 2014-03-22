using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task Push(IEnumerable<Achievement> achievements)
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
                    var comments = (await _fitocracy.GetWorkoutComments(group.Key))
                        .ToDictionary(pair => pair.Value, pair => (long?) pair.Key);

                    foreach (var achievement in commentAchievements)
                    {
                        long? commentId;
                        if (!comments.TryGetValue(achievement.CommentText, out commentId))
                        {
                            throw new Exception("TODO: unable to resolve comment ID");
                        }

                        achievement.CommentId = commentId;
                        _database.Update(achievement);
                    }
                }
            }
        }
    }
}