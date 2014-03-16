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

        public async Task<IEnumerable<string>> Execute(Workout workout)
        {
            var comments = new List<string>();

            foreach (var group in _grouping.GetAll())
            {
                Achievement freshAchievement = null;
                var sets = workout.Activities
                                  .Where(activity => group.Includes(activity.Name))
                                  .SelectMany(activity => activity.Sets);
                foreach (var set in sets)
                {
                    switch (group.Category)
                    {
                        case ActitivityCategory.Cardio:
                            {
                                var speed = set.Speed ?? (set.Distance/set.Duration);
                                var distance = set.Distance ?? (set.Speed*set.Duration);
                                if (speed == null || distance == null)
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
                                    "and a.[Distance] >= @distance", new {workout.UserId, workout.Date, group.Name, distance});
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
                                        "and coalesce(s.[Distance], s.[Speed]*s.[Duration]) >= @distance", new {workout.UserId, workout.Date, distance});
                                }

                                if (speed > previousMax)
                                {
                                    freshAchievement = new Achievement
                                        {
                                            WorkoutId = workout.Id,
                                            Type = "QualifiedRecord",
                                            Group = group.Name,
                                            Speed = speed,
                                            Distance = distance
                                        };
                                    comments.Add(string.Format("{0} speed record: {1:N} km/h over {2:N} km or greater", group.Name, speed*3.6M, distance/1000));
                                }
                            }
                            break;
                        case ActitivityCategory.Weights:
                            {
                                var repetitions = set.Repetitions;
                                var weight = set.Weight;
                                if (repetitions == null || weight == null)
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
                                    "and a.[Weight] >= @weight", new {workout.UserId, workout.Date, group.Name, Weight = weight});
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
                                        "and s.[Weight] >= @weight", new {workout.UserId, workout.Date, Weight = weight});
                                }

                                if (repetitions > previousMax)
                                {
                                    freshAchievement = new Achievement
                                        {
                                            WorkoutId = workout.Id,
                                            Type = "QualifiedRecord",
                                            Group = group.Name,
                                            Repetitions = repetitions,
                                            Weight = weight
                                        };
                                    comments.Add(string.Format("{0} repetitions record: {1:N} reps with {2:N} kg or greater", group.Name, repetitions, weight));
                                }
                            }
                            break;
                        default:
                            continue;
                    }
                }

                var staleAchievement = await _database.Single<Achievement>(
                    "select * " +
                    "from [Achievement] " +
                    "where [WorkoutId] = @Id " +
                    "and [Type] = 'QualifiedRecord' " +
                    "and [Group] = @Name", new {workout.Id, group.Name});
                if (freshAchievement != null)
                {
                    if (staleAchievement == null)
                    {
                        _database.Insert(freshAchievement);
                    }
                    else if (freshAchievement.Distance != staleAchievement.Distance ||
                             freshAchievement.Speed != staleAchievement.Speed ||
                             freshAchievement.Repetitions != staleAchievement.Repetitions ||
                             freshAchievement.Weight != staleAchievement.Weight)
                    {
                        freshAchievement.Id = staleAchievement.Id;
                        _database.Update(freshAchievement);
                    }
                }
                else if (staleAchievement != null)
                {
                    _database.Delete(staleAchievement);
                }
            }

            return comments;
        }
    }
}