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

            foreach (var group in workout.Activities.GroupBy(activity => activity.Group).Where(group => group.Key != null))
            {
                var category = _grouping.GetGroupCategory(group.Key);
                if (category == null)
                {
                    continue;
                }

                var sets = group.SelectMany(activity => activity.Sets).ToList();
                if (sets.Count <= 1)
                {
                    continue;
                }

                string column;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        column = "Distance";
                        break;
                    case ActivityCategory.Sports:
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
                    "and a.[Group] = @Key", new {workout.UserId, workout.Date, group.Key});
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
                        "  and a.[Group] = @Key " +
                        "  group by w.[Id] " +
                        ") [x]", new {workout.UserId, workout.Date, group.Key});
                }

                var sum = sets.Sum(set =>
                    {
                        switch (category)
                        {
                            case ActivityCategory.Cardio:
                                return set.Distance;
                            case ActivityCategory.Sports:
                                return set.Duration;
                            default:
                                return set.Repetitions;
                        }
                    });
                if (sum <= previousMax)
                {
                    continue;
                }

                var achievement = new Achievement
                    {
                        Type = "DailyRecord",
                        Group = group.Key
                    };
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        achievement.Distance = sum;
                        achievement.CommentText = string.Format("Daily {0} record: {1:N1} km", group.Key, sum/1000);
                        break;
                    case ActivityCategory.Sports:
                        achievement.Duration = sum;
                        var duration = TimeSpan.FromSeconds((double) sum.Value);
                        achievement.CommentText = string.Format("Daily {0} record: {1}",
                                                                group.Key,
                                                                string.Format(duration < TimeSpan.FromHours(1)
                                                                                  ? "{0:m\\:ss} minutes"
                                                                                  : "{0:h\\:mm} hours", duration));
                        break;
                    default:
                        achievement.Repetitions = sum;
                        achievement.CommentText = string.Format("Daily {0} record: {1:N0} reps", group.Key, sum);
                        break;
                }
                achievements.Add(achievement);
            }

            return achievements;
        }
    }
}