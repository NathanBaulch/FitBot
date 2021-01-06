using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public class AchievementPushService : IAchievementPushService
    {
        private readonly IFitocracyService _fitocracy;

        public AchievementPushService(IFitocracyService fitocracy) => _fitocracy = fitocracy;

        public async Task Push(IEnumerable<Achievement> achievements, CancellationToken cancel)
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
                    try
                    {
                        await _fitocracy.AddComment(achievement.WorkoutId, achievement.CommentText);
                    }
                    catch (Exception ex) when (ex.GetBaseException() is ApplicationException {Message: "Can only comment if member of this group"})
                    {
                        Trace.TraceWarning(ex.Message);
                    }
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}