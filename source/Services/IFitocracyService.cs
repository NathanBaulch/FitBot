using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IFitocracyService
    {
        long SelfUserId { get; }
        Task<IList<User>> GetFollowers(int pageNum = 0);
        Task<IList<Workout>> GetWorkouts(long userId, int offset = 0);
        Task<Workout> GetWorkout(long workoutId);
        Task AddComment(long workoutId, string text);
        Task DeleteComment(long commentId);
        Task GiveProp(long workoutId);
    }
}