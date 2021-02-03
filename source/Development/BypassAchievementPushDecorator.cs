using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementPushDecorator : IAchievementPushService
    {
        public BypassAchievementPushDecorator(IAchievementPushService _)
        {
        }

        public Task Push(User user, IEnumerable<Achievement> achievements, CancellationToken cancel) => Task.CompletedTask;
    }
}