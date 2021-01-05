using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementDecorator : IAchievementService
    {
        public BypassAchievementDecorator(IAchievementService _)
        {
        }

        public Task<IEnumerable<Achievement>> Process(User user, IEnumerable<Workout> workouts, CancellationToken cancel) => Task.FromResult(Enumerable.Empty<Achievement>());
    }
}