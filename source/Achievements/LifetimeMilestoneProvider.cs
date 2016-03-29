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
                //Cardio (km)
                {"Cycling", 1000},
                {"Farmer's Walk", 10},
                {"Rock Climbing", 1},
                {"Rope Climb", 2},
                {"Swimming", 100},
                {"Walking", 200},
                //Bodyweight (reps)
                {"Ab Wheel", 1000},
                {"Band Pull Apart", 5000},
                {"Bicep Clutch", 1000},
                {"Bird-Dog/Dead Bug", 1000},
                {"Body Weight Squat", 5000},
                {"Burpee", 5000},
                {"Dip", 5000},
                {"Dragon Flag", 1000},
                {"Glute March", 1000},
                {"Hand Gripper", 20000},
                {"Hand Switches", 10000},
                {"High Knees", 10000},
                {"Hip Adductor/Abductor", 5000},
                {"Hip Hinge", 1000},
                {"Iron Cross", 1000},
                {"Jacks", 5000},
                {"Jumps", 5000},
                {"Knee Tuck", 5000},
                {"Lunge", 5000},
                {"Mountain Climbers", 5000},
                {"Muscle-Up", 500},
                {"Other Bodyweight", 5000},
                {"Push-Up", 10000},
                {"Reverse Push-Up", 1000},
                {"Russian Twist", 5000},
                {"Scissors", 5000},
                {"Sit-Up", 10000},
                {"Step Up", 5000},
                {"Stir The Pot", 1000},
                {"Stretch", 5000},
                {"Walking Stairs", 10000},
                {"Walkout", 500},
                {"Ys", 1000},
                {"YTI Raises", 500},
                //Weights (reps)
                {"Atlas Stones", 1000},
                {"Ball Slam", 5000},
                {"Bench Press", 5000},
                {"Bent Press", 1000},
                {"Bicep Curl", 5000},
                {"Cable Crossover", 1000},
                {"Calf Raise", 5000},
                {"Flyes", 1000},
                {"Front Raise", 1000},
                {"Good Morning", 1000},
                {"High Pull", 1000},
                {"Jerk", 1000},
                {"Kettlebell Swing", 5000},
                {"Landmines", 1000},
                {"Long Cycle", 5000},
                {"Other Weightlifting", 5000},
                {"Pullover", 1000},
                {"Push Press", 1000},
                {"Rear Delt Raise", 1000},
                {"Rear Delt Row", 1000},
                {"Squat", 5000},
                {"Thruster", 1000},
                {"Turkish Get-Up", 1000},
                {"Woodchopper", 5000},
                //Sports (hours)
                {"Battle Ropes", 2},
                {"Elliptical Trainer", 20},
                {"Flexed-Arm Hang", 1},
                {"Flutter", 2},
                {"Foam Rolling", 10},
                {"Handstand/Headstand", 5},
                {"Heavy Bag", 20},
                {"Hill Training", 20},
                {"Hula Hooping", 10},
                {"Jump Rope", 5},
                {"Ladder Drills", 5},
                {"L-Sit", 1},
                {"Meditation", 20},
                {"Other Cardio", 200},
                {"Planche", 1},
                {"Plank", 2},
                {"Running Stairs", 10},
                {"Shoveling Snow", 20},
                {"Speed Bag", 10},
                {"Stair Machine", 20},
                {"Static Wall Sit", 2},
                {"Stretching", 10},
                {"Superman", 1}
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

            foreach (var group in workout.Activities.GroupBy(activity => activity.Group).Where(group => group.Key != null))
            {
                var category = _grouping.GetGroupCategory(group.Key);
                if (category == null)
                {
                    continue;
                }

                int threshold;
                if (!Thresholds.TryGetValue(group.Key, out threshold))
                {
                    switch (category)
                    {
                        case ActivityCategory.Cardio:
                            threshold = 500;
                            break;
                        case ActivityCategory.Sports:
                            threshold = 100;
                            break;
                        default:
                            threshold = 2000;
                            break;
                    }
                }

                string column;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        column = "Distance";
                        threshold *= 1000;
                        break;
                    case ActivityCategory.Sports:
                        column = "Duration";
                        threshold *= 3600;
                        break;
                    default:
                        column = "Repetitions";
                        break;
                }

                var previousSum = await _database.Single<decimal?>(
                    "select sum([" + column + "]) " +
                    "from [Workout] w, [Activity] a, [Set] s " +
                    "where w.[Id] = a.[WorkoutId] " +
                    "and a.[Id] = s.[ActivityId] " +
                    "and w.[UserId] = @UserId " +
                    "and w.[Date] < @Date " +
                    "and a.[Group] = @Key", new {workout.UserId, workout.Date, group.Key});
                if (previousSum == null)
                {
                    continue;
                }

                var sum = group.SelectMany(activity => activity.Sets)
                               .Sum(set =>
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
                                   }) + previousSum;
                if (sum < threshold)
                {
                    continue;
                }

                previousSum = Math.Floor(previousSum.Value/threshold)*threshold;
                sum = Math.Floor(sum.Value/threshold)*threshold;
                if (sum == previousSum)
                {
                    continue;
                }

                var achievement = new Achievement
                    {
                        Type = "LifetimeMilestone",
                        Group = group.Key
                    };
                string formattedValue;
                switch (category)
                {
                    case ActivityCategory.Cardio:
                        achievement.Distance = sum;
                        formattedValue = sum.FormatDistance();
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
                achievement.CommentText = $"Lifetime {group.Key} milestone: {formattedValue}";
                achievements.Add(achievement);
            }

            return achievements;
        }
    }
}