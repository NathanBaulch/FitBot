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
                    if (max == null || (max.Weight == null && max.Repetitions == null))
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
                        "and s.[Weight] " + (max.Weight != null ? ">= @Weight" : "is null") + " " +
                        "order by s.[Weight] desc, s.[Repetitions] desc", new {workout.UserId, fromDate, workout.Date, activity.Name, max.Weight});
                    if (thisYearMax != null && (max.Weight < thisYearMax.Weight || max.Repetitions <= thisYearMax.Repetitions))
                    {
                        continue;
                    }

                    var achievement = new Achievement
                        {
                            Type = "ComebackRecord",
                            Activity = activity.Name,
                            CommentText = string.Format("1 year {0} comeback record: ", activity.Name)
                        };
                    if (max.Weight == null)
                    {
                        achievement.Repetitions = max.Repetitions;
                        achievement.CommentText += max.Repetitions.FormatRepetitions();
                    }
                    else
                    {
                        achievement.Weight = max.Weight;
                        achievement.CommentText += max.Weight.FormatWeight(max.IsImperial);
                        if (thisYearMax != null && max.Weight == thisYearMax.Weight)
                        {
                            achievement.Repetitions = max.Repetitions;
                            achievement.CommentText += " for " + max.Repetitions.FormatRepetitions();
                        }
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

                    var max = activity.Sets
                                      .Select(set =>
                                          {
                                              switch (category)
                                              {
                                                  case ActivityCategory.Cardio:
                                                      return new {Value = set.Distance, set.IsImperial};
                                                  case ActivityCategory.Bodyweight:
                                                      return new {Value = set.Repetitions, set.IsImperial};
                                                  case ActivityCategory.Sports:
                                                      return new {Value = set.Duration, set.IsImperial};
                                                  default:
                                                      throw new ArgumentOutOfRangeException();
                                              }
                                          })
                                      .OrderByDescending(set => set.Value)
                                      .FirstOrDefault();
                    if (max == null || max.Value == null)
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
                    if (lastYearMax == null || max.Value > lastYearMax || max.Value*2 <= lastYearMax)
                    {
                        continue;
                    }

                    var thisYearCount = await _database.Single<int>(
                        "select count(*) " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] >= @fromDate " +
                        "and w.[Date] < @Date " +
                        "and a.[Name] = @Name " +
                        "and s.[" + column + "] >= @Value", new {workout.UserId, fromDate, workout.Date, activity.Name, max.Value});
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
                            achievement.Distance = max.Value;
                            formattedValue = max.Value.FormatDistance(max.IsImperial);
                            break;
                        case ActivityCategory.Bodyweight:
                            achievement.Repetitions = max.Value;
                            formattedValue = max.Value.FormatRepetitions();
                            break;
                        case ActivityCategory.Sports:
                            achievement.Duration = max.Value;
                            formattedValue = max.Value.FormatDuration();
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