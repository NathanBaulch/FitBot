using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class LifetimeMilestoneProvider : IAchievementProvider
    {
        //TODO: support more groups
        private static readonly IDictionary<string, int> Thresholds = new Dictionary<string, int>
            {
                //Cardio (meters)
                {"Cycling", 1000000},
                {"Running", 500000},
                {"Rowing", 500000},
                {"Walking", 200000},
                {"Swimming", 100000},
                //Bodyweight (reps)
                {"Push-Up", 2000},
                {"Pull-Up", 2000},
                {"Sit-Up", 4000},
                {"Dip", 2000},
                //Weights (reps)
                {"Pulldown", 2000},
                {"Shrug", 2000},
                {"Deadlift", 2000},
                {"Squat", 2000},
                //Sports (seconds)
                {"Volleyball", 360000},
                {"Football", 360000},
                {"Snowboarding", 360000},
                {"Surfing", 360000},
                {"Squash", 360000},
                {"Plank", 3600}
            };

        private readonly IDatabaseService _database;
        private readonly IActivityGroupingService _grouping;

        public LifetimeMilestoneProvider(IDatabaseService database, IActivityGroupingService grouping)
        {
            _database = database;
            _grouping = grouping;
        }

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var achievements = new List<Achievement>();

            foreach (var threshold in Thresholds)
            {
                var group = _grouping.Get(threshold.Key);
                if (workout.Activities.Count(activity => group.Includes(activity.Name)) > 0)
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
                    var sum = await _database.Single<decimal?>(
                        "select sum([" + column + "]) " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] <= @Date " +
                        "and " + group.BuildSqlFilter("a.[Name]"), new {workout.UserId, workout.Date});
                    if (sum >= threshold.Value)
                    {
                        sum = Math.Floor(sum.Value/threshold.Value)*threshold.Value;
                        if (await _database.Single<long?>(
                            "select top 1 a.[Id] " +
                            "from [Workout] w, [Achievement] a " +
                            "where w.[Id] = a.[WorkoutId] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] < @Date " +
                            "and a.[Type] = 'LifetimeMilestone' " +
                            "and a.[Group] = @Name " +
                            "and a.[" + column + "] = @sum", new {workout.UserId, workout.Date, group.Name, sum}) == null)
                        {
                            var achievement = new Achievement
                                {
                                    Type = "LifetimeMilestone",
                                    Group = group.Name
                                };
                            switch (group.Category)
                            {
                                case ActitivityCategory.Cardio:
                                    achievement.Distance = sum;
                                    achievement.CommentText = string.Format("Lifetime {0} milestone: {1:N0} km", group.Name, sum/1000);
                                    break;
                                case ActitivityCategory.Sports:
                                    achievement.Duration = sum;
                                    achievement.CommentText = string.Format("Lifetime {0} milestone: {1:N0} hours", group.Name, sum/3600);
                                    break;
                                default:
                                    achievement.Repetitions = sum;
                                    achievement.CommentText = string.Format("Lifetime {0} milestone: {1:N0} reps", group.Name, sum);
                                    break;
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