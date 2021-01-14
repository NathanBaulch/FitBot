using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using Microsoft.Extensions.Logging;

namespace FitBot.Services
{
    public class AchievementPushService : IAchievementPushService
    {
        private readonly IFitocracyService _fitocracy;
        private readonly ILogger<AchievementPushService> _logger;

        public AchievementPushService(IFitocracyService fitocracy, ILogger<AchievementPushService> logger)
        {
            _fitocracy = fitocracy;
            _logger = logger;
        }

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
                        _logger.LogWarning(ex.Message);
                    }
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}