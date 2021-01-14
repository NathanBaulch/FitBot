using System;
using System.Collections.Generic;
using System.Threading;
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

        public void Push(IEnumerable<Achievement> achievements, CancellationToken cancel)
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
                    try
                    {
                        _fitocracy.AddComment(achievement.WorkoutId, achievement.CommentText);
                    }
                    catch (ApplicationException ex) when (ex.Message == "Can only comment if member of this group")
                    {
                        _logger.LogWarning(ex.Message);
                    }
                }

                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}