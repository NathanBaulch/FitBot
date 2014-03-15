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
        private static readonly IDictionary<string, int> Thresholds = new Dictionary<string, int>
            {
                //Cardio (meters)
                {"Cycling", 1000000},
                {"Running", 500000},
                {"Rowing", 500000},
                {"Walking", 200000},
                {"Swimming", 100000},
                //Bodyweight (reps)
                {"Push-Up", 1000},
                {"Pull-Up", 1000},
                {"Sit-Up", 1000},
                {"Dip", 1000},
                //Weights (reps)
                {"Pulldown", 1000},
                {"Shrug", 1000},
                {"Deadlift", 1000},
                {"Squat", 1000},
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

        public async Task<IEnumerable<string>> Execute(Workout workout)
        {
            var comments = new List<string>();

            foreach (var threshold in Thresholds)
            {
                Achievement freshAchievement = null;
                var group = _grouping.Get(threshold.Key);
                if (workout.Activities.Count(activity => group.Includes(activity.Name)) > 1)
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
                        if (await _database.Single<int>(
                            "select count(*) " +
                            "from [Workout] w, [Achievement] a " +
                            "where w.[Id] = a.[WorkoutId] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] < @Date " +
                            "and a.[Type] = 'LifetimeMilestone' " +
                            "and a.[Group] = @Name " +
                            "and a.[" + column + "] = @sum", new {workout.UserId, workout.Date, group.Name, sum}) == 0)
                        {
                            freshAchievement = new Achievement
                                {
                                    WorkoutId = workout.Id,
                                    Type = "LifetimeMilestone",
                                    Group = group.Name
                                };
                            switch (group.Category)
                            {
                                case ActitivityCategory.Cardio:
                                    freshAchievement.Distance = sum;
                                    comments.Add(string.Format("Lifetime {0} distance milestone: {1:N} km", group.Name, sum/1000));
                                    break;
                                case ActitivityCategory.Sports:
                                    freshAchievement.Duration = sum;
                                    comments.Add(string.Format("Lifetime {0} duration milestone: {1:N} hours", group.Name, sum/3600));
                                    break;
                                default:
                                    freshAchievement.Repetitions = sum;
                                    comments.Add(string.Format("Lifetime {0} repetition milestone: {1:N} reps", group.Name, sum));
                                    break;
                            }
                        }
                    }
                }

                var staleAchievement = await _database.Single<Achievement>(
                    "select * " +
                    "from [Achievement] " +
                    "where [WorkoutId] = @Id " +
                    "and [Type] = 'LifetimeMilestone' " +
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