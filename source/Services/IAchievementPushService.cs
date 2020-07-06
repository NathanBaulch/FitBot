using System.Collections.Generic;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementPushService
    {
        void Push(IEnumerable<Achievement> achievements, CancellationToken cancel = default);
    }
}