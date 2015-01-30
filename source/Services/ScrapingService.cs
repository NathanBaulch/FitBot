using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using FitBot.Model;
using HtmlAgilityPack;

//TODO: error/warning handling

namespace FitBot.Services
{
    public class ScrapingService : IScrapingService
    {
        private const decimal MetersPerInch = 0.0254M;
        private const decimal MetersPerFoot = 0.3048M;
        private const decimal MetersPerYard = 0.9144M;
        private const decimal MetersPerFathom = 1.8288M;
        private const decimal MetersPerMile = 1609.344M;
        private const decimal KilogramsPerPound = 0.453592M;

        public IList<Workout> ExtractWorkouts(Stream content)
        {
            var doc = new HtmlDocument();
            doc.Load(content);
            return doc.DocumentNode
                      .Descendants("div")
                      .Where(div => div.GetAttributeValue("data-ag-type", null) == "workout")
                      .Select(ExtractWorkout)
                      .ToList();
        }

        private static Workout ExtractWorkout(HtmlNode node)
        {
            var value = node.Descendants("a")
                            .Select(a => a.GetAttributeValue("data-item-id", null))
                            .FirstOrDefault(item => item != null);
            long workoutId;
            if (!long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out workoutId))
            {
                throw new InvalidDataException("TODO: unable to find workout ID");
            }

            value = node.Descendants("a")
                        .Where(a => a.GetAttributeValue("class", null) == "action_time gray_link")
                        .Select(a => a.InnerText)
                        .FirstOrDefault();
            DateTime date;
            if (value == null || !DateTime.TryParse(value, out date))
            {
                throw new InvalidDataException("TODO: unable to find workout date");
            }

            value = node.Descendants("span")
                        .Where(span => span.GetAttributeValue("class", null) == "stream_total_points")
                        .Select(span => span.InnerText)
                        .FirstOrDefault();
            int points;
            if (value == null || !value.EndsWith(" pts") || !int.TryParse(value.Substring(0, value.Length - 4).Replace(".", ","), NumberStyles.Any, CultureInfo.InvariantCulture, out points))
            {
                Debug.Fail("TODO: unable to find workout points");
                points = 0;
            }

            return new Workout
                {
                    Id = workoutId,
                    Date = date,
                    Points = points,
                    Activities = node.Descendants("ul")
                                     .Where(ul => ul.GetAttributeValue("class", null) == "action_detail")
                                     .SelectMany(ul => ul.Descendants("li"))
                                     .Where(li => li.Elements("div").Any(div => div.GetAttributeValue("class", null) == "action_prompt") &&
                                                  li.Elements("div").All(div => div.GetAttributeValue("class", null) != "group_container"))
                                     .Select(ExtractActivity)
                                     .ToList()
                };
        }

        private static Activity ExtractActivity(HtmlNode node, int index)
        {
            var name = node.Descendants("div")
                           .Where(div => div.GetAttributeValue("class", null) == "action_prompt")
                           .Select(div => HtmlEntity.DeEntitize(div.InnerText).Trim().Replace("  ", " "))
                           .FirstOrDefault();
            if (name == null)
            {
                throw new InvalidDataException("TODO: unable to find activity name");
            }

            return new Activity
                {
                    Sequence = index,
                    Name = name,
                    Note = node.Descendants("li")
                               .Where(li => li.GetAttributeValue("class", null) == "stream_note")
                               .Select(li => HtmlEntity.DeEntitize(li.InnerText).Trim())
                               .FirstOrDefault(),
                    Sets = node.Descendants("li")
                               .Where(li => li.GetAttributeValue("class", null) != "stream_note")
                               .Select(ExtractSet)
                               .ToList()
                };
        }

        private static Set ExtractSet(HtmlNode node, int index)
        {
            var value = node.Descendants("span")
                            .Where(span => span.GetAttributeValue("class", null) == "action_prompt_points")
                            .Select(span => span.InnerText)
                            .FirstOrDefault();
            int points;
            if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out points))
            {
                Debug.Fail("TODO: unable to find set points");
            }

            var set = new Set
                {
                    Sequence = index,
                    Points = points
                };

            var text = HtmlEntity.DeEntitize(node.FirstChild.InnerText).Trim();
            if (text.EndsWith("(PR)"))
            {
                set.IsPr = true;
                text = text.Substring(0, text.Length - 4).Trim();
            }

            decimal? weight = null;
            decimal? distance = null;
            decimal? speed = null;
            var metricCount = 0;
            var imperialCount = 0;
            if (!string.IsNullOrEmpty(text))
            {
                var assisted = false;
                foreach (var part in text.Replace(" x ", " | ").Split('|').Select(part => part.Trim()))
                {
                    var pos = part.IndexOf(' ');
                    if (pos < 0)
                    {
                        TimeSpan duration;
                        if (TimeSpan.TryParse(part, out duration))
                        {
                            if (duration > TimeSpan.Zero)
                            {
                                set.Duration = (decimal) duration.TotalSeconds;
                            }
                            continue;
                        }

                        switch (part.ToLowerInvariant())
                        {
                            case "assisted":
                                assisted = true;
                                continue;
                            case "weighted":
                                continue;
                        }
                    }
                    else
                    {
                        decimal num;
                        if (decimal.TryParse(part.Substring(0, pos), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                        {
                            if (num == 0)
                            {
                                continue;
                            }

                            var metric = part.Substring(pos + 1);
                            switch (metric.ToLowerInvariant())
                            {
                                case "reps":
                                case "jumps":
                                case "holes":
                                case "slams":
                                case "floors":
                                case "throws":
                                case "jumping jacks":
                                    set.Repetitions = (int) num;
                                    continue;

                                case "kg":
                                    weight = (assisted ? -1 : 1)*num;
                                    metricCount++;
                                    continue;
                                case "lb":
                                    weight = (assisted ? -1 : 1)*num*KilogramsPerPound;
                                    imperialCount++;
                                    continue;

                                case "m":
                                    distance = num;
                                    metricCount++;
                                    continue;
                                case "cm":
                                    distance = num*0.01M;
                                    metricCount++;
                                    continue;
                                case "laps (25m)":
                                    distance = num*25;
                                    metricCount++;
                                    continue;
                                case "laps (50m)":
                                    distance = num*50;
                                    metricCount++;
                                    continue;
                                case "km":
                                    distance = num*1000;
                                    metricCount++;
                                    continue;
                                case "in":
                                    distance = num*MetersPerInch;
                                    imperialCount++;
                                    continue;
                                case "ft":
                                    distance = num*MetersPerFoot;
                                    imperialCount++;
                                    continue;
                                case "yd":
                                    distance = num*MetersPerYard;
                                    imperialCount++;
                                    continue;
                                case "fathoms":
                                    distance = num*MetersPerFathom;
                                    imperialCount++;
                                    continue;
                                case "mi":
                                    distance = num*MetersPerMile;
                                    imperialCount++;
                                    continue;

                                case "m/s":
                                    speed = num;
                                    metricCount++;
                                    continue;
                                case "km/hr":
                                    speed = num/3.6M;
                                    metricCount++;
                                    continue;
                                case "fps":
                                    speed = num*MetersPerFoot;
                                    imperialCount++;
                                    continue;
                                case "mph":
                                    speed = num*MetersPerMile/3600;
                                    imperialCount++;
                                    continue;
                                case "min/100m":
                                    speed = 5/(3*num);
                                    metricCount++;
                                    continue;
                                case "split":
                                    speed = 25/(3*num);
                                    metricCount++;
                                    continue;
                                case "min/km":
                                    speed = 50/(3*num);
                                    metricCount++;
                                    continue;
                                case "sec/lap (25m)":
                                    speed = 25/num;
                                    metricCount++;
                                    continue;
                                case "sec/lap (50m)":
                                    speed = 50/num;
                                    metricCount++;
                                    continue;
                                case "min/mi":
                                    speed = MetersPerMile/(60*num);
                                    imperialCount++;
                                    continue;

                                case "bpm":
                                    set.HeartRate = num;
                                    continue;

                                case "%":
                                case "and 3/4-inch band": //workaround
                                    break;

                                default:
                                    Debug.Fail("TODO: unrecognized set metric: " + metric);
                                    continue;
                            }
                        }
                    }

                    set.Difficulty = part;
                }
            }

            set.Weight = Round(weight);
            set.Distance = Round(distance);
            set.Speed = Round(speed);
            set.IsImperial = imperialCount > metricCount;
            return set;
        }

        private static decimal? Round(decimal? value)
        {
            return value != null ? Math.Round(value.Value, 2, MidpointRounding.AwayFromZero) : (decimal?) null;
        }

        public IDictionary<long, string> ExtractWorkoutComments(Stream content, long selfUserId)
        {
            var doc = new HtmlDocument();
            doc.Load(content);
            return doc.DocumentNode
                      .Descendants("li")
                      .Where(li => li.GetAttributeValue("data-user-id", null) == selfUserId.ToString(CultureInfo.InvariantCulture))
                      .Select(ExtractComment)
                      .ToDictionary(comment => comment.Item1, comment => comment.Item2);
        }

        private static Tuple<long, string> ExtractComment(HtmlNode node)
        {
            var value = node.GetAttributeValue("data-comment-id", null);
            long commentId;
            if (!long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out commentId))
            {
                throw new InvalidDataException("TODO: unable to find comment ID");
            }

            return new Tuple<long, string>(
                commentId,
                node.Descendants("span")
                    .Where(span => span.GetAttributeValue("class", null) == "comment-copy")
                    .Select(span => HtmlEntity.DeEntitize(span.InnerText).Trim())
                    .FirstOrDefault());
        }
    }
}