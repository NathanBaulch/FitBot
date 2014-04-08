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
                //Bodyweight (reps)
                {"Ab Wheel", 1000},
                {"Band Pull Apart", 5000},
                {"Bicep Clutch", 1000},
                {"Bird-Dog/Dead Bug", 1000},
                {"Body Weight Squat", 5000},
                {"Burpee", 5000},
                {"Dragon Flag", 1000},
                {"Glute March", 1000},
                {"Hand Gripper", 20000},
                {"Hand Switches", 10000},
                {"High Knees", 10000},
                {"Hip Adductor/Abductor", 5000},
                {"Hip Hinge", 1000},
                {"Iron Cross", 1000},
                {"Jacks", 10000},
                {"Jumps", 10000},
                {"Knee Tuck", 5000},
                {"Lunge", 5000},
                {"Mountain Climbers", 5000},
                {"Muscle-Up", 500},
                {"Other Bodyweight", 20000}, //TODO: investigate this
                {"Push-Up", 10000},
                {"Reverse Push-Up", 1000},
                {"Russian Twist", 5000},
                {"Scissors", 5000},
                {"Sit-Up", 10000},
                {"Step Up", 5000},
                {"Stir The Pot", 50000}, //TODO: investigate this
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
                {"Deadlift", 5000},
                {"Flyes", 1000},
                {"Good Morning", 1000},
                {"High Pull", 1000},
                {"Jerk", 1000},
                {"Kettlebell Swing", 5000},
                {"Landmines", 1000},
                {"Long Cycle", 5000},
                {"Other Weightlifting", 5000}, //TODO: investigate this
                {"Pullover", 1000},
                {"Push Press", 1000},
                {"Rear Delt Raise", 1000},
                {"Rear Delt Row", 1000},
                {"Squat", 5000},
                {"Thruster", 1000},
                {"Turkish Get-Up", 1000},
                {"Woodchopper", 5000},
                //Sports (hours)
                {"American Football", 50},
                {"Archery", 50},
                {"Badminton", 50},
                {"Baseball", 10},
                {"Basketball", 50},
                {"Battle Ropes", 2},
                {"Belly Boarding", 50},
                {"Boxing", 50},
                {"Class", 2000}, //TODO: investigate this
                {"Dodgeball", 20},
                {"Elliptical Trainer", 20},
                {"Fencing", 500}, //TODO: investigate this
                {"Flexed-Arm Hang", 1},
                {"Flutter", 2},
                {"Foam Rolling", 10},
                {"Football", 50},
                {"Frisbee", 50},
                {"General Program", 50},
                {"Golf", 50},
                {"Gymnastics", 20},
                {"Handstand/Headstand", 5},
                {"Heavy Bag", 20},
                {"Hill Training", 20},
                {"Hockey", 20},
                {"Horseback Riding", 50},
                {"Hula Hooping", 10},
                {"Jump Rope", 5},
                {"Ladder Drills", 5},
                {"Lake Canoeing", 10},
                {"L-Sit", 1},
                {"Meditation", 20},
                {"Other Cardio", 50}, //TODO: investigate this
                {"Paddleboarding", 50},
                {"Ping Pong", 50},
                {"Planche", 5},
                {"Plank", 2},
                {"Racquetball", 20},
                {"Rugby", 50},
                {"Shoveling snow", 20},
                {"Skateboarding", 20},
                {"Skating", 50},
                {"Skiing", 200}, //TODO: investigate this
                {"Softball", 50},
                {"Speed Bag", 10},
                {"Squash", 20},
                {"Stair Machine", 20},
                {"Static Wall Sit", 2},
                {"Stretching", 10},
                {"Superman", 1},
                {"Surfing", 500}, //TODO: investigate this
                {"Tennis", 50},
                {"Trampoline", 20},
                {"Wakeboarding", 20},
                {"Water Polo", 50},
                {"Wrestling", 50},
                {"Yoga", 50}
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

            foreach (var group in _grouping.GetAll().Where(group => workout.Activities.Count(activity => group.Includes(activity.Name)) > 0))
            {
                int threshold;
                if (!Thresholds.TryGetValue(group.Name, out threshold))
                {
                    switch (group.Category)
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
                switch (group.Category)
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

                var sum = await _database.Single<decimal?>(
                    "select sum([" + column + "]) " +
                    "from [Workout] w, [Activity] a, [Set] s " +
                    "where w.[Id] = a.[WorkoutId] " +
                    "and a.[Id] = s.[ActivityId] " +
                    "and w.[UserId] = @UserId " +
                    "and w.[Date] <= @Date " +
                    "and " + group.BuildSqlFilter("a.[Name]"), new {workout.UserId, workout.Date});
                if (sum == null || sum < threshold)
                {
                    continue;
                }

                sum = Math.Floor(sum.Value/threshold)*threshold;
                if (await _database.Single<long?>(
                    "select top 1 a.[Id] " +
                    "from [Workout] w, [Achievement] a " +
                    "where w.[Id] = a.[WorkoutId] " +
                    "and w.[UserId] = @UserId " +
                    "and w.[Date] < @Date " +
                    "and a.[Type] = 'LifetimeMilestone' " +
                    "and a.[Group] = @Name " +
                    "and a.[" + column + "] = @sum", new {workout.UserId, workout.Date, group.Name, sum}) != null)
                {
                    continue;
                }

                var achievement = new Achievement
                    {
                        Type = "LifetimeMilestone",
                        Group = group.Name
                    };
                switch (group.Category)
                {
                    case ActivityCategory.Cardio:
                        achievement.Distance = sum;
                        achievement.CommentText = string.Format("Lifetime {0} milestone: {1:N0} km", group.Name, sum/1000);
                        break;
                    case ActivityCategory.Sports:
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

            return achievements;
        }
    }
}