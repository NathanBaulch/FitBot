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
                                                    Speed = set.Speed ?? (set.Distance/set.Duration),
                                                    Distance = Truncate(set.Distance ?? (set.Speed*set.Duration))
                                                })
                                            .Where(set => set.Speed != null && set.Distance != null)
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

                                if (set.Speed > previousMax)
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
                                            .Where(set => set.Repetitions != null && set.Weight != null)
                                            .Select(set => new
                                                {
                                                    set.Id,
                                                    set.IsImperial,
                                                    set.Repetitions,
                                                    Weight = Truncate(set.Weight)
                                                })
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

        private static decimal? Truncate(decimal? value)
        {
            if (value == null || value == 0)
            {
                return value;
            }

            var scale = (decimal) Math.Pow(10, Math.Floor(Math.Log10(Math.Abs((double) value.Value))));
            return scale*Math.Truncate(value.Value/scale);
        }
    }
}