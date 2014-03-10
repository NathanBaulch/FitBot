using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Achievements
{
    public interface IAchievementProvider
    {
        Task<IEnumerable<string>> Execute(Workout workout);
    }
}