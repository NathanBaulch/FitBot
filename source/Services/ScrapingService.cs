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

        public IList<Workout> ExtractWorkouts(Stream content, User selfUser)
        {
            var doc = new HtmlDocument();
            doc.Load(content);
            return doc.DocumentNode
                      .Descendants("div")
                      .Where(div => div.GetAttributeValue("data-ag-type", null) == "workout")
                      .Select(node => ExtractWorkout(node, selfUser))
                      .ToList();
        }

        private static Workout ExtractWorkout(HtmlNode node, User selfUser)
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
                        .Where(a => a.GetAttributeValue("class", null) == "stream_total_points")
                        .Select(a => a.InnerText)
                        .FirstOrDefault();
            int points;
            if (value == null || !value.EndsWith(" pts") || !int.TryParse(value.Substring(0, value.Length - 4), NumberStyles.Any, CultureInfo.InvariantCulture, out points))
            {
                Debug.Fail("TODO: unable to find workout points");
                points = 0;
            }

            long? commentId;
            if (selfUser.Id != 0)
            {
                value = node.Descendants("li")
                            .Where(li => li.GetAttributeValue("data-user-id", null) == selfUser.Id.ToString(CultureInfo.InvariantCulture))
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
                    IsPropped = node.Descendants("span")
                                    .Where(span => span.GetAttributeValue("class", null) == "individual_prop_" + workoutId)
                                    .SelectMany(span => span.Elements("a"))
                                    .Any(a => a.InnerText == selfUser.Username),
                    Activities = node.Descendants("ul")
                                     .Where(div => div.GetAttributeValue("class", null) == "action_detail")
                                     .SelectMany(ul => ul.Descendants("li"))
                                     .Where(li => li.Elements("div").Any(div => div.GetAttributeValue("class", null) == "action_prompt") &&
                                                  li.Elements("div").All(div => div.GetAttributeValue("class", null) != "group_container"))
                                     .Select(ExtractActivity)
                                     .Where(activity => activity != null)
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
                throw new InvalidDataException("TODO");
            }

            var note = node.Descendants("li")
                           .Where(div => div.GetAttributeValue("class", null) == "stream_note")
                           .Select(div => HtmlEntity.DeEntitize(div.InnerText))
                           .FirstOrDefault();
            return new Activity
                {
                    Sequence = index,
                    Name = name,
                    Note = note,
                    Sets = node.Descendants("li")
                               .Where(li => li.GetAttributeValue("class", null) != "stream_note")
                               .Select(ExtractSet)
                               .ToList()
                };
        }

        private static Set ExtractSet(HtmlNode node, int index)
        {
            var value = node.Descendants("span")
                            .Where(div => div.GetAttributeValue("class", null) == "action_prompt_points")
                            .Select(div => div.InnerText)
                            .FirstOrDefault();
            int points;
            if (!int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out points))
            {
                Debug.Fail("TODO: unable to find action points");
            }

            var action = new Set
                {
                    Sequence = index,
                    Points = points
                };

            var text = node.FirstChild.InnerText.Trim();
            if (text.EndsWith("(PR)"))
            {
                action.IsPr = true;
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
                            action.Duration = (int) duration.TotalSeconds;
                            continue;
                        }
                    }
                    else
                    {
                        decimal num;
                        if (decimal.TryParse(value.Substring(0, pos), NumberStyles.Any, CultureInfo.InvariantCulture, out num))
                        {
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
                                    action.Repetitions = (int) num;
                                    break;

                                case "kg":
                                    action.Weight = num;
                                    break;
                                case "lb":
                                    action.Weight = num*KilogramsPerPound;
                                    break;

                                case "m":
                                    action.Distance = num;
                                    break;
                                case "cm":
                                    action.Distance = num*0.01M;
                                    break;
                                case "laps (25m)":
                                    action.Distance = num*25;
                                    break;
                                case "laps (50m)":
                                    action.Distance = num*50;
                                    break;
                                case "km":
                                    action.Distance = num*1000;
                                    break;
                                case "in":
                                    action.Distance = num*MetersPerInch;
                                    break;
                                case "ft":
                                    action.Distance = num*MetersPerFoot;
                                    break;
                                case "yd":
                                    action.Distance = num*MetersPerYard;
                                    break;
                                case "fathoms":
                                    action.Distance = num*MetersPerFathom;
                                    break;
                                case "mi":
                                    action.Distance = num*MetersPerMile;
                                    break;

                                case "km/hr":
                                    action.Speed = num;
                                    break;
                                case "m/s":
                                    action.Speed = num*3.6M;
                                    break;
                                case "fps":
                                    action.Speed = num*3.6M*MetersPerFoot;
                                    break;
                                case "mph":
                                    action.Speed = num*0.001M*MetersPerMile;
                                    break;
                                case "min/100m":
                                    action.Speed = 6/num;
                                    break;
                                case "split":
                                    action.Speed = 30/num;
                                    break;
                                case "min/km":
                                    action.Speed = 60/num;
                                    break;
                                case "sec/lap (25m)":
                                    action.Speed = 90/num;
                                    break;
                                case "sec/lap (50m)":
                                    action.Speed = 180/num;
                                    break;
                                case "min/mi":
                                    action.Speed = 0.06M*MetersPerMile/num;
                                    break;

                                case "bpm":
                                    action.HeartRate = num;
                                    break;

                                case "%":
                                    action.Incline = num;
                                    break;

                                    //TODO: workaround
                                case "and 3/4-inch band":
                                    action.Difficulty = value;
                                    break;

                                default:
                                    Debug.Fail("TODO: unrecognized action metric: " + metric);
                                    break;
                            }
                            continue;
                        }
                    }

                    action.Difficulty = value;
                }
            }

            return action;
        }
    }
}