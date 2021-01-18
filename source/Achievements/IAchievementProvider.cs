using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Achievements
{
    public interface IAchievementProvider
    {
        IEnumerable<Achievement> Execute(Workout workout);
    }
}