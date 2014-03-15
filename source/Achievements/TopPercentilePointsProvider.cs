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
        private readonly IFitocracyService _fitocracy;

        public TopPercentilePointsProvider(IDatabaseService database, IFitocracyService fitocracy)
        {
            _database = database;
            _fitocracy = fitocracy;
        }

        public async Task<IEnumerable<string>> Execute(Workout workout)
        {
            var offset = (int) (await _database.GetWorkoutCount(workout.UserId)*Percentile);
            var isPropped = false;
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
                    await _fitocracy.GiveProp(workout.Id);
                    isPropped = true;
                }
            }

            if (workout.IsPropped != isPropped)
            {
                workout.IsPropped = isPropped;
                _database.Update(workout);
            }

            return Enumerable.Empty<string>();
        }
    }
}