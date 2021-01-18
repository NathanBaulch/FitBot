using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IFitocracyService
    {
        Task<long> GetSelfUserId(CancellationToken cancel = default);
        Task<IList<User>> GetFollowers(int pageNum = 0, CancellationToken cancel = default);
        Task<IList<Workout>> GetWorkouts(long userId, int offset = 0, CancellationToken cancel = default);
        Task<Workout> GetWorkout(long workoutId, CancellationToken cancel = default);
        Task AddComment(long workoutId, string text, CancellationToken cancel = default);
        Task DeleteComment(long commentId, CancellationToken cancel = default);
        Task GiveProp(long workoutId, CancellationToken cancel = default);
    }
}