using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IActivityHashService
    {
        int Hash(IEnumerable<Activity> activities);
    }
}