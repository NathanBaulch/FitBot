using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IFitocracyService
    {
        Task<IList<User>> GetFollowers(int pageNum = 0);
        Task<IList<Workout>> GetWorkouts(long userId, int offset = 0);
        Task PostComment(long workoutId, string text);
        Task DeleteComment(long commentId);
        Task GiveProp(long workoutId);
    }
}