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

                if (category == ActivityCategory.Weights)
                {
                    var max = activity.Sets
                                      .OrderByDescending(set => set.Weight)
                                      .ThenByDescending(set => set.Repetitions)
                                      .FirstOrDefault();
                    if (max == null)
                    {
                        continue;
                    }

                    var fromDate = workout.Date.AddYears(-1);
                    var lastYearMax = await _database.Single<dynamic>(
                        "select top 1 s.[Weight], s.[Repetitions] " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] < @fromDate " +
                        "and a.[Name] = @Name " +
                        "order by s.[Weight] desc, s.[Repetitions] desc", new {workout.UserId, fromDate, activity.Name});
                    if (lastYearMax == null ||
                        max.Weight > lastYearMax.Weight ||
                        (max.Weight == lastYearMax.Weight && max.Repetitions > lastYearMax.Repetitions) ||
                        max.Weight*2 <= lastYearMax.Weight)
                    {
                        continue;
                    }

                    var thisYearMax = await _database.Single<dynamic>(
                        "select top 1 s.[Weight], s.[Repetitions] " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] >= @fromDate " +
                        "and w.[Date] < @Date " +
                        "and a.[Name] = @Name " +
                        "and s.[Weight] >= @Weight " +
                        "order by s.[Weight] desc, s.[Repetitions] desc", new {workout.UserId, fromDate, workout.Date, activity.Name, max.Weight});
                    if (thisYearMax != null && (max.Weight < thisYearMax.Weight || max.Repetitions <= thisYearMax.Repetitions))
                    {
                        continue;
                    }

                    var achievement = new Achievement
                        {
                            Type = "ComebackRecord",
                            Activity = activity.Name,
                            Weight = max.Weight,
                            Repetitions = max.Repetitions,
                            CommentText = string.Format("1 year {0} comeback record: {1}", activity.Name, max.Weight.FormatWeight())
                        };
                    if (thisYearMax != null && max.Weight == thisYearMax.Weight)
                    {
                        achievement.CommentText += " for " + max.Repetitions.FormatRepetitions();
                    }
                    achievements.Add(achievement);
                }
                else
                {
                    string column;
                    switch (category)
                    {
                        case ActivityCategory.Cardio:
                            column = "Distance";
                            break;
                        case ActivityCategory.Bodyweight:
                            column = "Repetitions";
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
                    if (lastYearMax == null || max > lastYearMax || max*2 <= lastYearMax)
                    {
                        continue;
                    }

                    var thisYearCount = await _database.Single<long>(
                        "select count(*) " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] >= @fromDate " +
                        "and w.[Date] < @Date " +
                        "and a.[Name] = @Name " +
                        "and s.[" + column + "] >= @max", new {workout.UserId, fromDate, workout.Date, activity.Name, max});
                    if (thisYearCount > 0)
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
            }

            return achievements;
        }
    }
}