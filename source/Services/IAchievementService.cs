using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementService
    {
        IEnumerable<Achievement> Process(User user, IEnumerable<Workout> workouts);
    }
}