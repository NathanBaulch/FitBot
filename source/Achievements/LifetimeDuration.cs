using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class LifetimeDuration : IAchievementProvider
    {
        private static readonly IDictionary<string[], int> Groups = new Dictionary<string[], int>
            {
                {new[] {"Volleyball"}, 360000},
                {new[] {"Football"}, 360000},
                {new[] {"Snowboarding"}, 360000},
                {new[] {"Surfing"}, 360000},
                {new[] {"Squash"}, 360000},
                {new[] {"Plank"}, 3600}
            };

        private readonly IDatabaseService _database;

        public LifetimeDuration(IDatabaseService database)
        {
            _database = database;
        }

        public async Task<IEnumerable<string>> Execute(Workout workout)
        {
            var comments = new List<string>();

            foreach (var group in Groups)
            {
                var type = GetType().Name;
                var groupStr = string.Join("/", group.Key);
                Achievement freshAchievement = null;
                if ((await _database.Single<int>(
                    "select count(*) " +
                    "from [Activity] " +
                    "where [WorkoutId] = @Id " +
                    "and " + string.Format("({0})", string.Join(" or ", group.Key.Select(item => string.Format("[Name] like '%{0}%'", item)))), new {workout.Id})) > 0)
                {
                    var sum = await _database.Single<int?>(
                        "select sum([Duration]) " +
                        "from [Workout] w, [Activity] a, [Set] s " +
                        "where w.[Id] = a.[WorkoutId] " +
                        "and a.[Id] = s.[ActivityId] " +
                        "and w.[UserId] = @UserId " +
                        "and w.[Date] <= @Date " +
                        "and " + string.Format("({0})", string.Join(" or ", group.Key.Select(item => string.Format("a.[Name] like '%{0}%'", item)))), new {workout.UserId, workout.Date});
                    if (sum >= group.Value)
                    {
                        var quantity = sum.Value/group.Value*group.Value;
                        if (await _database.Single<int>(
                            "select count(*) " +
                            "from [Achievement] a, [Workout] w " +
                            "where a.[WorkoutId] = w.[Id] " +
                            "and w.[UserId] = @UserId " +
                            "and w.[Date] < @Date " +
                            "and a.[Type] = @type " +
                            "and a.[Group] = @groupStr " +
                            "and a.[Quantity1] = @quantity", new {workout.UserId, workout.Date, type, groupStr, quantity}) == 0)
                        {
                            freshAchievement = new Achievement
                                {
                                    WorkoutId = workout.Id,
                                    Type = type,
                                    Group = groupStr,
                                    Quantity1 = quantity
                                };
                            comments.Add(string.Format("Lifetime {0} duration milestone: {1:N} hours", group, quantity/3600));
                        }
                    }
                }

                var staleAchievement = await _database.Single<Achievement>(
                    "select * " +
                    "from [Achievement] " +
                    "where [WorkoutId] = @Id " +
                    "and [Type] = @type " +
                    "and [Group] = @groupStr", new {workout.Id, type, groupStr});
                if (freshAchievement != null)
                {
                    if (staleAchievement == null)
                    {
                        _database.Insert(freshAchievement);
                    }
                    else if (freshAchievement.Quantity1 != staleAchievement.Quantity1 ||
                             freshAchievement.Quantity2 != staleAchievement.Quantity2)
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