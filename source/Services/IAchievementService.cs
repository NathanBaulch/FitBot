using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementService
    {
        Task<IEnumerable<Achievement>> Process(User user, IEnumerable<Workout> workouts);
    }
}