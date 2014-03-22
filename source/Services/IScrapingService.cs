using System.Collections.Generic;
using System.IO;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IScrapingService
    {
        IList<Workout> ExtractWorkouts(Stream content);
        IDictionary<long, string> ExtractWorkoutComments(Stream content, long selfUserId);
    }
}