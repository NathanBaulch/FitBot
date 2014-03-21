using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    internal class TopPercentilePointsProvider : IAchievementProvider
    {
        private const decimal Percentile = 0.05M;

        private readonly IDatabaseService _database;

        public TopPercentilePointsProvider(IDatabaseService database)
        {
            _database = database;
        }

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var offset = (int) (await _database.GetWorkoutCount(workout.UserId)*Percentile);
            if (offset > 0)
            {
                var threshold = await _database.Single<int>(
                    "select [Points] " +
                    "from [Workout] " +
                    "where [UserId] = @UserId " +
                    "order by [Points] desc " +
                    "offset @offset rows " +
                    "fetch next 1 rows only", new {workout.UserId, offset});
                if (workout.Points > threshold)
                {
                    return new[]
                        {
                            new Achievement
                                {
                                    Type = "TopPercentilePoints",
                                    IsPropped = true
                                }
                        };
                }
            }

            return Enumerable.Empty<Achievement>();
        }
    }
}