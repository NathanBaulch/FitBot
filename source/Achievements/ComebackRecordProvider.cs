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

            foreach (var group in _grouping.GetAll())
            {
                string column;
                switch (group.Category)
                {
                    case ActitivityCategory.Cardio:
                        column = "Distance";
                        break;
                    case ActitivityCategory.Bodyweight:
                        column = "Repetitions";
                        break;
                    case ActitivityCategory.Weights:
                        column = "Weight";
                        break;
                    case ActitivityCategory.Sports:
                        column = "Duration";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                var activities = workout.Activities
                                        .Where(activity => group.Includes(activity.Name) && !activity.Sets.Any(set => set.IsPr))
                                        .ToList();
                foreach (var activity in activities)
                {
                    var max = activity.Sets.Max(set =>
                        {
                            switch (group.Category)
                            {
                                case ActitivityCategory.Cardio:
                                    return set.Distance;
                                case ActitivityCategory.Bodyweight:
                                    return set.Repetitions;
                                case ActitivityCategory.Weights:
                                    return set.Weight;
                                case ActitivityCategory.Sports:
                                    return set.Duration;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        });
                    var fromDate = workout.Date.AddYears(-1);
                    var lastYearMax = await _database.Single<decimal?>(
                        "select max(s.[" + column + "]) " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] < @fromDate " +
                        "and a.[Name] = @Name", new {workout.UserId, fromDate, activity.Name});
                    if (lastYearMax != null)
                    {
                        var thisYearMax = await _database.Single<decimal?>(
                            "select max(s.[" + column + "]) " +
                            "from [Workout] w, [Activity] a, [Set] s " +
                            "where w.[Id] = a.[WorkoutId] " +
                            "and a.[Id] = s.[ActivityId] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] >= @fromDate " +
                            "and w.[Date] < @Date " +
                            "and a.[Name] = @Name", new {workout.UserId, fromDate, workout.Date, activity.Name});
                        if (max > (thisYearMax ?? 0) && max < lastYearMax && max*2 > lastYearMax)
                        {
                            var achievement = new Achievement
                                {
                                    Type = "ComebackRecord",
                                    Group = activity.Name
                                };
                            switch (group.Category)
                            {
                                case ActitivityCategory.Cardio:
                                    achievement.Distance = max;
                                    achievement.CommentText = string.Format("1 year {0} comeback record: {1:N1} km", activity.Name, max/1000);
                                    break;
                                case ActitivityCategory.Bodyweight:
                                    achievement.Repetitions = max;
                                    achievement.CommentText = string.Format("1 year {0} comeback record: {1:N0} reps", activity.Name, max);
                                    break;
                                case ActitivityCategory.Weights:
                                    achievement.Weight = max;
                                    achievement.CommentText = string.Format("1 year {0} comeback record: {1:N1} kg", activity.Name, max);
                                    break;
                                case ActitivityCategory.Sports:
                                    achievement.Duration = max;
                                    var duration = TimeSpan.FromSeconds((double) max.Value);
                                    achievement.CommentText = string.Format("1 year {0} comeback record: {1}",
                                                                            group.Name,
                                                                            string.Format(duration < TimeSpan.FromHours(1)
                                                                                              ? "{0:m\\:ss} minutes"
                                                                                              : "{0:h\\:mm} hours", duration));
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            achievements.Add(achievement);
                        }
                    }
                }
            }

            return achievements;
        }
    }
}