using System;
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

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var achievements = new List<Achievement>();

            foreach (var group in _grouping.GetAll())
            {
                var activities = workout.Activities
                                        .Where(activity => group.Includes(activity.Name))
                                        .ToList();
                if (activities.Count > 1)
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
                            "  and " + group.BuildSqlFilter("a.[Name]") + " " +
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
                    if (sum > previousMax)
                    {
                        var achievement = new Achievement
                            {
                                Type = "DailyRecord",
                                Group = group.Name
                            };
                        switch (group.Category)
                        {
                            case ActitivityCategory.Cardio:
                                achievement.Distance = sum;
                                achievement.CommentText = string.Format("Daily {0} record: {1:N1} km", group.Name, sum/1000);
                                break;
                            case ActitivityCategory.Sports:
                                achievement.Duration = sum;
                                var duration = TimeSpan.FromSeconds((double) sum.Value);
                                achievement.CommentText = string.Format("Daily {0} record: {1}",
                                                                        group.Name,
                                                                        string.Format(duration < TimeSpan.FromHours(1)
                                                                                          ? "{0:m\\:ss} minutes"
                                                                                          : "{0:h\\:mm} hours", duration));
                                break;
                            default:
                                achievement.Repetitions = sum;
                                achievement.CommentText = string.Format("Daily {0} record: {1:N0} reps", group.Name, sum);
                                break;
                        }
                        achievements.Add(achievement);
                    }
                }
            }

            return achievements;
        }
    }
}