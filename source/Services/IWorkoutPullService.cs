using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IWorkoutPullService
    {
        Task<IEnumerable<Workout>> Pull(User user, CancellationToken cancel = default);
    }
}