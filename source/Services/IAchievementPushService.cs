using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IAchievementPushService
    {
        Task Push(IEnumerable<Achievement> achievements);
    }
}