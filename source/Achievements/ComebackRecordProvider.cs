using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class ComebackRecordProvider : IAchievementProvider
    {
        private readonly IDatabaseService _database;
        private readonly IActivityGroupingService _grouping;

        public ComebackRecordProvider(IDatabaseService database, IActivityGroupingService grouping)
        {
            _database = database;
            _grouping = grouping;
        }

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var achievements = new List<Achievement>();

            foreach (var activity in workout.Activities.Where(activity => activity.Group != null && !activity.Sets.Any(set => set.IsPr)))
            {
                var category = _grouping.GetGroupCategory(activity.Group);
                if (category == null)
                {
                    continue;
                }

                string column;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        column = "Distance";
                        break;
                    case ActivityCategory.Bodyweight:
                        column = "Repetitions";
                        break;
                    case ActivityCategory.Weights:
                        column = "Weight";
                        break;
                    case ActivityCategory.Sports:
                        column = "Duration";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var max = activity.Sets.Max(set =>
                    {
                        switch (category)
                        {
                            case ActivityCategory.Cardio:
                                return set.Distance;
                            case ActivityCategory.Bodyweight:
                                return set.Repetitions;
                            case ActivityCategory.Weights:
                                return set.Weight;
                            case ActivityCategory.Sports:
                                return set.Duration;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    });
                if (max == null)
                {
                    continue;
                }

                var fromDate = workout.Date.AddYears(-1);
                var lastYearMax = await _database.Single<decimal?>(
                    "select max(s.[" + column + "]) " +
                    "from [Workout] w, [Activity] a, [Set] s " +
                    "where w.[Id] = a.[WorkoutId] " +
                    "and a.[Id] = s.[ActivityId] " +
                    "and w.[UserId] = @UserId " +
                    "and w.[Date] < @fromDate " +
                    "and a.[Name] = @Name", new {workout.UserId, fromDate, activity.Name});
                if (lastYearMax == null)
                {
                    continue;
                }

                var thisYearMax = await _database.Single<decimal?>(
                    "select max(s.[" + column + "]) " +
                    "from [Workout] w, [Activity] a, [Set] s " +
                    "where w.[Id] = a.[WorkoutId] " +
                    "and a.[Id] = s.[ActivityId] " +
                    "and w.[UserId] = @UserId " +
                    "and w.[Date] >= @fromDate " +
                    "and w.[Date] < @Date " +
                    "and a.[Name] = @Name", new {workout.UserId, fromDate, workout.Date, activity.Name});
                if (max <= (thisYearMax ?? 0) || max >= lastYearMax || max*2 <= lastYearMax)
                {
                    continue;
                }

                var achievement = new Achievement
                    {
                        Type = "ComebackRecord",
                        Activity = activity.Name
                    };
                string formattedValue;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        achievement.Distance = max;
                        formattedValue = max.FormatDistance();
                        break;
                    case ActivityCategory.Bodyweight:
                        achievement.Repetitions = max;
                        formattedValue = max.FormatRepetitions();
                        break;
                    case ActivityCategory.Weights:
                        achievement.Weight = max;
                        formattedValue = max.FormatWeight();
                        break;
                    case ActivityCategory.Sports:
                        achievement.Duration = max;
                        formattedValue = max.FormatDuration();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                achievement.CommentText = string.Format("1 year {0} comeback record: {1}", activity.Name, formattedValue);
                achievements.Add(achievement);
            }

            return achievements;
        }
    }
}