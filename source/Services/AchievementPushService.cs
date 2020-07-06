using System.Collections.Generic;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public class AchievementPushService : IAchievementPushService
    {
        private readonly IFitocracyService _fitocracy;

        public AchievementPushService(IFitocracyService fitocracy)
        {
            _fitocracy = fitocracy;
        }

        public void Push(IEnumerable<Achievement> achievements, CancellationToken cancel = default)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.IsPropped)
                {
                    _fitocracy.GiveProp(achievement.WorkoutId);
                }

                if (achievement.CommentId != null)
                {
                    _fitocracy.DeleteComment(achievement.CommentId.Value);
                }

                if (achievement.CommentText != null)
                {
                    _fitocracy.AddComment(achievement.WorkoutId, achievement.CommentText);
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}