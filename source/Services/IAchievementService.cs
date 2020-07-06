using System.Collections.Generic;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementService
    {
        IEnumerable<Achievement> Process(User user, IEnumerable<Workout> workouts, CancellationToken cancel = default);
    }
}