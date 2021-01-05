using System.Collections.Generic;
using System.Threading;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementPushDecorator : IAchievementPushService
    {
        public BypassAchievementPushDecorator(IAchievementPushService _)
        {
        }

        public void Push(IEnumerable<Achievement> achievements, CancellationToken cancel)
        {
        }
    }
}