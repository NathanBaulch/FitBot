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
                if (sum == 0)
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
                if ((previousMax ?? 0) == 0 || sum <= previousMax)
                {
                    continue;
                }

                var achievement = new Achievement
                    {
                        Type = "DailyRecord",
                        Group = group.Key
                    };
                string formattedValue;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        achievement.Distance = sum;
                        var lookup = sets.ToLookup(set => set.IsImperial);
                        formattedValue = sum.FormatDistance(lookup[true].Count() > lookup[false].Count());
                        break;
                    case ActivityCategory.Sports:
                        achievement.Duration = sum;
                        formattedValue = sum.FormatDuration();
                        break;
                    default:
                        achievement.Repetitions = sum;
                        formattedValue = sum.FormatRepetitions();
                        break;
                }
                achievement.CommentText = string.Format("Daily {0} record: {1}", group.Key, formattedValue);
                achievements.Add(achievement);
            }

            return achievements;
        }
    }
}