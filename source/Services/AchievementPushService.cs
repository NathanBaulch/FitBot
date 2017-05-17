using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task Push(IEnumerable<Achievement> achievements, CancellationToken cancel = default(CancellationToken))
        {
            foreach (var achievement in achievements)
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

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}