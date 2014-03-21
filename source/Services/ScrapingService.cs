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

        public IList<Workout> ExtractWorkouts(Stream content, long selfUserId)
        {
            var doc = new HtmlDocument();
            doc.Load(content);
            return doc.DocumentNode
                      .Descendants("div")
                      .Where(div => div.GetAttributeValue("data-ag-type", null) == "workout")
                      .Select(node => ExtractWorkout(node, selfUserId))
                      .ToList();
        }

        private static Workout ExtractWorkout(HtmlNode node, long selfUserId)
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
            if (value == null || !value.EndsWith(" pts") || !int.TryParse(value.Substring(0, value.Length - 4), NumberStyles.Any, CultureInfo.InvariantCulture, out points))
            {
                Debug.Fail("TODO: unable to find workout points");
                points = 0;
            }

            long? commentId;
            if (selfUserId != 0)
            {
                value = node.Descendants("li")
                            .Where(li => li.GetAttributeValue("data-user-id", null) == selfUserId.ToString(CultureInfo.InvariantCulture))
                            .Select(li => li.GetAttributeValue("data-comment-id", null))
                            .FirstOrDefault();
                long id;
                commentId = long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out id) ? id : (long?) null;
            }
            else
            {
                commentId = null;
            }

            return new Workout
                {
                    Id = workoutId,
                    Date = date,
                    Points = points,
                    CommentId = commentId,
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
                           .Select(div => HtmlEntity.DeEntitize(div.InnerText).Replace("  ", " "))
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
                               .Select(li => HtmlEntity.DeEntitize(li.InnerText))
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

            if (!string.IsNullOrEmpty(text))
            {
                foreach (var part in text.Replace(" x ", " | ").Split('|'))
                {
                    value = part.Trim();

                    var pos = value.IndexOf(' ');
                    if (pos < 0)
                    {
                        TimeSpan duration;
                        if (TimeSpan.TryParse(value, out duration))
                        {
                            if (duration > TimeSpan.Zero)
                            {
                                set.Duration = (decimal) duration.TotalSeconds;
                            }
                            continue;
                        }
                    }
                    else
                    {
                        decimal num;
                        if (decimal.TryParse(value.Substring(0, pos), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                        {
                            if (num == 0)
                            {
                                continue;
                            }

                            var metric = value.Substring(pos + 1);
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
                                    break;

                                case "kg":
                                    set.Weight = num;
                                    break;
                                case "lb":
                                    set.Weight = num*KilogramsPerPound;
                                    break;

                                case "m":
                                    set.Distance = num;
                                    break;
                                case "cm":
                                    set.Distance = num*0.01M;
                                    break;
                                case "laps (25m)":
                                    set.Distance = num*25;
                                    break;
                                case "laps (50m)":
                                    set.Distance = num*50;
                                    break;
                                case "km":
                                    set.Distance = num*1000;
                                    break;
                                case "in":
                                    set.Distance = num*MetersPerInch;
                                    break;
                                case "ft":
                                    set.Distance = num*MetersPerFoot;
                                    break;
                                case "yd":
                                    set.Distance = num*MetersPerYard;
                                    break;
                                case "fathoms":
                                    set.Distance = num*MetersPerFathom;
                                    break;
                                case "mi":
                                    set.Distance = num*MetersPerMile;
                                    break;

                                case "m/s":
                                    set.Speed = num;
                                    break;
                                case "km/hr":
                                    set.Speed = num/3.6M;
                                    break;
                                case "fps":
                                    set.Speed = num*MetersPerFoot;
                                    break;
                                case "mph":
                                    set.Speed = num*MetersPerMile/3600;
                                    break;
                                case "min/100m":
                                    set.Speed = 5/(3*num);
                                    break;
                                case "split":
                                    set.Speed = 25/(3*num);
                                    break;
                                case "min/km":
                                    set.Speed = 50/(3*num);
                                    break;
                                case "sec/lap (25m)":
                                    set.Speed = 25/num;
                                    break;
                                case "sec/lap (50m)":
                                    set.Speed = 50/num;
                                    break;
                                case "min/mi":
                                    set.Speed = MetersPerMile/(60*num);
                                    break;

                                case "bpm":
                                    set.HeartRate = num;
                                    break;

                                case "%":
                                    set.Incline = num;
                                    break;

                                    //TODO: workaround
                                case "and 3/4-inch band":
                                    set.Difficulty = value;
                                    break;

                                default:
                                    Debug.Fail("TODO: unrecognized set metric: " + metric);
                                    break;
                            }
                            continue;
                        }
                    }

                    set.Difficulty = value;
                }
            }

            return set;
        }
    }
}