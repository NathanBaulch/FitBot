using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementDecorator : IAchievementService
    {
        public BypassAchievementDecorator(IAchievementService _)
        {
        }

        public IEnumerable<Achievement> Process(User user, IEnumerable<Workout> workouts, CancellationToken cancel) => Enumerable.Empty<Achievement>();
    }
}