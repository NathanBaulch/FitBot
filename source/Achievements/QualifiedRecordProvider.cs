﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Achievements
{
    public class QualifiedRecordProvider : IAchievementProvider
    {
        private const decimal KilometersPerMile = 1.609344M;
        private const decimal KilogramsPerPound = 0.453592M;

        private readonly IDatabaseService _database;
        private readonly IActivityGroupingService _grouping;

        public QualifiedRecordProvider(IDatabaseService database, IActivityGroupingService grouping)
        {
            _database = database;
            _grouping = grouping;
        }

        public async Task<IEnumerable<Achievement>> Execute(Workout workout)
        {
            var achievements = new List<Achievement>();

            foreach (var group in workout.Activities.GroupBy(activity => activity.Group).Where(group => group.Key != null))
            {
                switch (_grouping.GetGroupCategory(group.Key))
                {
                    case ActivityCategory.Cardio:
                        {
                            var sets = group.SelectMany(activity => activity.Sets)
                                            .Select(set => new
                                                {
                                                    set.Id,
                                                    set.IsImperial,
                                                    Speed = set.Speed ?? Round(set.Distance/set.Duration),
                                                    Distance = Truncate(set.Distance ?? (set.Speed*set.Duration), set.IsImperial ? KilometersPerMile : 0)
                                                })
                                            .Where(set => (set.Speed ?? 0) > 0 && (set.Distance ?? 0) >= 1000)
                                            .ToList();
                            foreach (var set in sets)
                            {
                                if (sets.Any(item => item != set &&
                                                     ((item.Distance >= set.Distance && item.Speed > set.Speed) ||
                                                      (item.Distance > set.Distance && item.Speed == set.Speed) ||
                                                      (item.Distance == set.Distance && item.Speed == set.Speed && item.Id > set.Id))))
                                {
                                    continue;
                                }

                                var previousMax = await _database.Single<decimal?>(
                                    "select max(a.[Speed]) " +
                                    "from [Workout] w, [Achievement] a " +
                                    "where w.[Id] = a.[WorkoutId] " +
                                    "and w.[UserId] = @UserId " +
                                    "and w.[Date] < @Date " +
                                    "and a.[Type] = 'QualifiedRecord' " +
                                    "and a.[Group] = @Key " +
                                    "and a.[Distance] >= @Distance", new {workout.UserId, workout.Date, group.Key, set.Distance});
                                if (previousMax == null)
                                {
                                    previousMax = await _database.Single<decimal?>(
                                        "select max(coalesce(s.[Speed], s.[Distance]/s.[Duration])) " +
                                        "from [Workout] w, [Activity] a, [Set] s " +
                                        "where w.[Id] = a.[WorkoutId] " +
                                        "and a.[Id] = s.[ActivityId] " +
                                        "and w.[UserId] = @UserId " +
                                        "and w.[Date] < @Date " +
                                        "and a.[Group] = @Key " +
                                        "and coalesce(s.[Distance], s.[Speed]*s.[Duration]) >= @Distance", new {workout.UserId, workout.Date, group.Key, set.Distance});
                                }

                                if (set.Speed > Round(previousMax))
                                {
                                    achievements.Add(
                                        new Achievement
                                            {
                                                Type = "QualifiedRecord",
                                                Group = group.Key,
                                                Speed = set.Speed,
                                                Distance = set.Distance,
                                                CommentText = string.Format("Qualified {0} record: {1} for {2} or more", group.Key, set.Speed.FormatSpeed(set.IsImperial), set.Distance.FormatDistance(set.IsImperial))
                                            });
                                }
                            }
                        }
                        break;
                    case ActivityCategory.Weights:
                        {
                            var sets = group.SelectMany(activity => activity.Sets)
                                            .Select(set => new
                                                {
                                                    set.Id,
                                                    set.IsImperial,
                                                    set.Repetitions,
                                                    Weight = Truncate(set.Weight, set.IsImperial ? KilogramsPerPound : 0)
                                                })
                                            .Where(set => (set.Repetitions ?? 0) > 0 && (set.Weight ?? 0) >= 1)
                                            .ToList();
                            foreach (var set in sets)
                            {
                                if (sets.Any(item => item != set &&
                                                     ((item.Weight >= set.Weight && item.Repetitions > set.Repetitions) ||
                                                      (item.Weight > set.Weight && item.Repetitions == set.Repetitions) ||
                                                      (item.Weight == set.Weight && item.Repetitions == set.Repetitions && item.Id > set.Id))))
                                {
                                    continue;
                                }

                                var previousMax = await _database.Single<decimal?>(
                                    "select max(a.[Repetitions]) " +
                                    "from [Workout] w, [Achievement] a " +
                                    "where w.[Id] = a.[WorkoutId] " +
                                    "and w.[UserId] = @UserId " +
                                    "and w.[Date] < @Date " +
                                    "and a.[Type] = 'QualifiedRecord' " +
                                    "and a.[Group] = @Key " +
                                    "and a.[Weight] >= @Weight", new {workout.UserId, workout.Date, group.Key, set.Weight});
                                if (previousMax == null)
                                {
                                    previousMax = await _database.Single<decimal?>(
                                        "select max(s.[Repetitions]) " +
                                        "from [Workout] w, [Activity] a, [Set] s " +
                                        "where w.[Id] = a.[WorkoutId] " +
                                        "and a.[Id] = s.[ActivityId] " +
                                        "and w.[UserId] = @UserId " +
                                        "and w.[Date] < @Date " +
                                        "and a.[Group] = @Key " +
                                        "and s.[Weight] >= @Weight", new {workout.UserId, workout.Date, group.Key, set.Weight});
                                }

                                if (set.Repetitions > previousMax)
                                {
                                    achievements.Add(
                                        new Achievement
                                            {
                                                Type = "QualifiedRecord",
                                                Group = group.Key,
                                                Repetitions = set.Repetitions,
                                                Weight = set.Weight,
                                                CommentText = string.Format("Qualified {0} record: {1} at {2} or more", group.Key, set.Repetitions.FormatRepetitions(), set.Weight.FormatWeight(set.IsImperial))
                                            });
                                }
                            }
                        }
                        break;
                }
            }

            return achievements;
        }

        private static decimal? Round(decimal? value)
        {
            return value != null ? Math.Round(value.Value, 2, MidpointRounding.AwayFromZero) : (decimal?) null;
        }

        private static decimal? Truncate(decimal? value, decimal scale)
        {
            if (value == null || value == 0)
            {
                return value;
            }

            if (scale != 0)
            {
                value = Math.Round(value.Value/scale, 2, MidpointRounding.AwayFromZero);
                if (value == 0)
                {
                    return 0;
                }
            }

            var order = (decimal) Math.Pow(10, Math.Floor(Math.Log10(Math.Abs((double) value))));
            value = order*Math.Truncate(value.Value/order);

            if (scale != 0)
            {
                value = Math.Round(scale*value.Value, 2, MidpointRounding.AwayFromZero);
            }

            return value;
        }
    }
}