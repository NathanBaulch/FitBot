using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class DailyRecordProvider : IAchievementProvider
    {
        private readonly IDatabaseService _database;
        private readonly IActivityGroupingService _grouping;

        public DailyRecordProvider(IDatabaseService database, IActivityGroupingService grouping)
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
                var activities = workout.Activities
                                        .Where(activity => group.Includes(activity.Name))
                                        .ToList();
                if (activities.Count > 0)
                {
                    string column;
                    switch (group.Category)
                    {
                        case ActitivityCategory.Cardio:
                            column = "Distance";
                            break;
                        case ActitivityCategory.Sports:
                            column = "Duration";
                            break;
                        default:
                            column = "Repetitions";
                            break;
                    }
                    var previousMax = await _database.Single<decimal?>(
                        "select max(a.[" + column + "]) " +
                        "from [Workout] w, [Achievement] a " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] < @Date " +
                        "and a.[Type] = 'DailyRecord' " +
                        "and a.[Group] = @Name", new {workout.UserId, workout.Date, group.Name});
                    var filter = group.BuildSqlFilter("a.[Name]");
                    if (previousMax == null)
                    {
                        previousMax = await _database.Single<decimal?>(
                            "select max([Value]) " +
                            "from ( " +
                            "  select sum(s.[" + column + "]) [Value] " +
                            "  from [Workout] w, [Activity] a, [Set] s " +
                            "  where w.[Id] = a.[WorkoutId] " +
                            "  and a.[Id] = s.[ActivityId] " +
                            "  and w.[UserId] = @UserId " +
                            "  and w.[Date] < @Date " +
                            "  and " + filter + " " +
                            "  group by w.[Id] " +
                            ") [x]", new {workout.UserId, workout.Date});
                    }
                    var sum = activities.SelectMany(activity => activity.Sets)
                                        .Sum(set =>
                                            {
                                                switch (group.Category)
                                                {
                                                    case ActitivityCategory.Cardio:
                                                        return set.Distance;
                                                    case ActitivityCategory.Sports:
                                                        return set.Duration;
                                                    default:
                                                        return set.Repetitions;
                                                }
                                            });
                    if (previousMax == null || sum > previousMax)
                    {
                        freshAchievement = new Achievement
                            {
                                WorkoutId = workout.Id,
                                Type = "DailyRecord",
                                Group = group.Name
                            };
                        switch (group.Category)
                        {
                            case ActitivityCategory.Cardio:
                                freshAchievement.Distance = sum;
                                comments.Add(string.Format("Daily {0} distance record: {1:N} km", group.Name, sum/1000));
                                break;
                            case ActitivityCategory.Sports:
                                freshAchievement.Duration = sum;
                                comments.Add(string.Format("Daily {0} duration record: {1:N} hours", group.Name, sum/3600));
                                break;
                            default:
                                freshAchievement.Repetitions = sum;
                                comments.Add(string.Format("Daily {0} repetition record: {1:N} reps", group.Name, sum));
                                break;
                        }
                    }
                }

                var staleAchievement = await _database.Single<Achievement>(
                    "select * " +
                    "from [Achievement] " +
                    "where [WorkoutId] = @Id " +
                    "and [Type] = 'DailyRecord' " +
                    "and [Group] = @Name", new {workout.Id, group.Name});
                if (freshAchievement != null)
                {
                    if (staleAchievement == null)
                    {
                        _database.Insert(freshAchievement);
                    }
                    else if (freshAchievement.Distance != staleAchievement.Distance ||
                             freshAchievement.Repetitions != staleAchievement.Repetitions ||
                             freshAchievement.Duration != staleAchievement.Duration)
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