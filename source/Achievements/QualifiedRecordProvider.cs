using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class QualifiedRecordProvider : IAchievementProvider
    {
        private readonly IDatabaseService _database;
        private readonly IActivityGroupingService _grouping;

        public QualifiedRecordProvider(IDatabaseService database, IActivityGroupingService grouping)
        {
            _database = database;
            _grouping = grouping;
        }

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var achievements = new List<Achievement>();

            foreach (var group in _grouping.GetAll())
            {
                switch (group.Category)
                {
                    case ActitivityCategory.Cardio:
                        {
                            var sets = workout.Activities
                                              .Where(activity => group.Includes(activity.Name))
                                              .SelectMany(activity => activity.Sets)
                                              .Select(set => new
                                                  {
                                                      set.Id,
                                                      Speed = set.Speed ?? (set.Distance/set.Duration),
                                                      Distance = set.Distance ?? (set.Speed*set.Duration)
                                                  })
                                              .Where(set => set.Speed != null && set.Distance != null)
                                              .ToList();
                            foreach (var set in sets)
                            {
                                if (sets.Any(item => item != set &&
                                                     ((item.Distance >= set.Distance && item.Speed > set.Speed) ||
                                                      (item.Distance > set.Distance && item.Speed == set.Speed) ||
                                                      (item.Distance == set.Distance && item.Speed == set.Speed && item.Id > set.Id))))
                                {
                                    continue;
                                }

                                var previousMax = await _database.Single<decimal?>(
                                    "select max(a.[Speed]) " +
                                    "from [Workout] w, [Achievement] a " +
                                    "where w.[Id] = a.[WorkoutId] " +
                                    "and w.[UserId] = @UserId " +
                                    "and w.[Date] < @Date " +
                                    "and a.[Type] = 'QualifiedRecord' " +
                                    "and a.[Group] = @Name " +
                                    "and a.[Distance] >= @Distance", new {workout.UserId, workout.Date, group.Name, set.Distance});
                                if (previousMax == null)
                                {
                                    previousMax = await _database.Single<decimal?>(
                                        "select max(coalesce(s.[Speed], s.[Distance]/s.[Duration])) " +
                                        "from [Workout] w, [Activity] a, [Set] s " +
                                        "where w.[Id] = a.[WorkoutId] " +
                                        "and a.[Id] = s.[ActivityId] " +
                                        "and w.[UserId] = @UserId " +
                                        "and w.[Date] < @Date " +
                                        "and " + group.BuildSqlFilter("a.[Name]") + " " +
                                        "and coalesce(s.[Distance], s.[Speed]*s.[Duration]) >= @Distance", new {workout.UserId, workout.Date, set.Distance});
                                }

                                if (set.Speed > previousMax)
                                {
                                    achievements.Add(
                                        new Achievement
                                            {
                                                Type = "QualifiedRecord",
                                                Group = group.Name,
                                                Speed = set.Speed,
                                                Distance = set.Distance,
                                                CommentText = string.Format("{0} Speed record: {1:N} km/h over {2:N} km or greater", group.Name, set.Speed*3.6M, set.Distance/1000)
                                            });
                                }
                            }
                        }
                        break;
                    case ActitivityCategory.Weights:
                        {
                            var sets = workout.Activities
                                              .Where(activity => group.Includes(activity.Name))
                                              .SelectMany(activity => activity.Sets)
                                              .Where(set => set.Repetitions != null && set.Weight != null)
                                              .ToList();
                            foreach (var set in sets)
                            {
                                if (sets.Any(item => item != set &&
                                                     ((item.Weight >= set.Weight && item.Repetitions > set.Repetitions) ||
                                                      (item.Weight > set.Weight && item.Repetitions == set.Repetitions) ||
                                                      (item.Weight == set.Weight && item.Repetitions == set.Repetitions && item.Id > set.Id))))
                                {
                                    continue;
                                }

                                var previousMax = await _database.Single<decimal?>(
                                    "select max(a.[Repetitions]) " +
                                    "from [Workout] w, [Achievement] a " +
                                    "where w.[Id] = a.[WorkoutId] " +
                                    "and w.[UserId] = @UserId " +
                                    "and w.[Date] < @Date " +
                                    "and a.[Type] = 'QualifiedRecord' " +
                                    "and a.[Group] = @Name " +
                                    "and a.[Weight] >= @Weight", new {workout.UserId, workout.Date, group.Name, set.Weight});
                                if (previousMax == null)
                                {
                                    previousMax = await _database.Single<decimal?>(
                                        "select max(s.[Repetitions]) " +
                                        "from [Workout] w, [Activity] a, [Set] s " +
                                        "where w.[Id] = a.[WorkoutId] " +
                                        "and a.[Id] = s.[ActivityId] " +
                                        "and w.[UserId] = @UserId " +
                                        "and w.[Date] < @Date " +
                                        "and " + group.BuildSqlFilter("a.[Name]") + " " +
                                        "and s.[Weight] >= @Weight", new {workout.UserId, workout.Date, set.Weight});
                                }

                                if (set.Repetitions > previousMax)
                                {
                                    achievements.Add(
                                        new Achievement
                                            {
                                                Type = "QualifiedRecord",
                                                Group = group.Name,
                                                Repetitions = set.Repetitions,
                                                Weight = set.Weight,
                                                CommentText = string.Format("{0} repetitions record: {1:N} reps with {2:N} kg or greater", group.Name, set.Repetitions, set.Weight)
                                            });
                                }
                            }
                        }
                        break;
                }
            }

            return achievements;
        }
    }
}