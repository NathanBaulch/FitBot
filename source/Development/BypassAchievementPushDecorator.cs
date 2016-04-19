using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementPushDecorator : IAchievementPushService
    {
        public BypassAchievementPushDecorator(IAchievementPushService decorated)
        {
        }

        public Task Push(IEnumerable<Achievement> achievements, CancellationToken cancel = default(CancellationToken))
        {
            return Task.FromResult<object>(null);
        }
    }
}