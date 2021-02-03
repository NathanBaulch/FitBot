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
        private readonly IDatabaseService _database;
        private readonly ILogger<AchievementPushService> _logger;

        public AchievementPushService(IFitocracyService fitocracy, IDatabaseService database, ILogger<AchievementPushService> logger)
        {
            _fitocracy = fitocracy;
            _database = database;
            _logger = logger;
        }

        public async Task Push(User user, IEnumerable<Achievement> achievements, CancellationToken cancel)
        {
            await Push(achievements, cancel);
            var cutoff = new DateTime(Math.Max(user.InsertDate.AddDays(-7).Ticks, DateTime.UtcNow.AddDays(-30).Ticks));
            await Push(_database.GetUnpushedAchievements(user.Id, cutoff), cancel);
        }

        private async Task Push(IEnumerable<Achievement> achievements, CancellationToken cancel)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.IsPropped)
                {
                    await _fitocracy.GiveProp(achievement.WorkoutId, cancel);
                }

                if (achievement.CommentId != null)
                {
                    await _fitocracy.DeleteComment(achievement.CommentId.Value, cancel);
                }

                if (achievement.CommentText != null)
                {
                    try
                    {
                        await _fitocracy.AddComment(achievement.WorkoutId, achievement.CommentText, cancel);
                    }
                    catch (Exception ex) when (ex.GetBaseException() is ApplicationException {Message: "Can only comment if member of this group"})
                    {
                        _logger.LogWarning(ex.Message);
                    }
                }

                _database.UpdateIsPushed(achievement.Id, true);
                cancel.ThrowIfCancellationRequested();
            }
        }
    }
}