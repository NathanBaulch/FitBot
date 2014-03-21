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
                    var previousMax = await _database.Single<decimal?>(
                        "select max(a.[" + column + "]) " +
                        "from [Workout] w, [Achievement] a " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] >= @fromDate " +
                        "and w.[Date] < @Date " +
                        "and a.[Type] = 'ComebackRecord' " +
                        "and a.[Group] = @Name", new {workout.UserId, fromDate, workout.Date, activity.Name});
                    if (previousMax == null &&
                        (await _database.Single<long?>(
                            "select top 1 a.[Id] " +
                            "from [Workout] w, [Activity] a " +
                            "where w.[Id] = a.[WorkoutId] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] < @fromDate " +
                            "and a.[Name] = @Name", new {workout.UserId, fromDate, activity.Name})) != null)
                    {
                        previousMax = await _database.Single<decimal?>(
                            "select max(s.[" + column + "]) " +
                            "from [Workout] w, [Activity] a, [Set] s " +
                            "where w.[Id] = a.[WorkoutId] " +
                            "and a.[Id] = s.[ActivityId] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] >= @fromDate " +
                            "and w.[Date] < @Date " +
                            "and a.[Name] = @Name", new {workout.UserId, fromDate, workout.Date, activity.Name});
                    }
                    if (max > previousMax)
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
                                achievement.CommentText = string.Format("{0} 1 year comeback record: {1:N} km", activity.Name, max/1000);
                                break;
                            case ActitivityCategory.Bodyweight:
                                achievement.Repetitions = max;
                                achievement.CommentText = string.Format("{0} 1 year comeback record: {1:N} reps", activity.Name, max);
                                break;
                            case ActitivityCategory.Weights:
                                achievement.Weight = max;
                                achievement.CommentText = string.Format("{0} 1 year comeback record: {1:N} kg", activity.Name, max);
                                break;
                            case ActitivityCategory.Sports:
                                achievement.Duration = max;
                                achievement.CommentText = string.Format("{0} 1 year comeback record: {1:N} hours", activity.Name, max/3600);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        achievements.Add(achievement);
                    }
                }
            }

            return achievements;
        }
    }
}