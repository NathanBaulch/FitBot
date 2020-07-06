using System.Collections.Generic;
using System.Threading;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IWorkoutPullService
    {
        IEnumerable<Workout> Pull(User user, CancellationToken cancel = default);
    }
}