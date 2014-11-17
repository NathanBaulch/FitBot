using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BypassAchievementDecorator : IAchievementService
    {
        public BypassAchievementDecorator(IAchievementService decorated)
        {
        }

        public Task<IEnumerable<Achievement>> Process(User user, IEnumerable<Workout> workouts)
        {
            return Task.FromResult(Enumerable.Empty<Achievement>());
        }
    }
}