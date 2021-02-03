using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementPushService
    {
        Task Push(User user, IEnumerable<Achievement> achievements, CancellationToken cancel = default);
    }
}