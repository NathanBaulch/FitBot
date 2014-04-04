using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FitBot.Services
{
    public interface IActivityGroupingService
    {
        IEnumerable<ActivityGroup> GetAll();
        ActivityGroup Get(string name);
    }

    public class ActivityGroup
    {
        private readonly ActitivityCategory _category;
        private readonly string _name;
        private readonly IList<string> _includeStrings;
        private readonly IList<string> _excludeStrings;
        private readonly string _sqlTemplate;
        private readonly IDictionary<string, bool> _includesCache;

        public ActivityGroup(ActitivityCategory category, string name, IList<string> includeStrings, IList<string> excludeStrings)
        {
            _category = category;
            _name = name;
            _includeStrings = includeStrings;
            _excludeStrings = excludeStrings ?? new string[0];

            var template = string.Format("({0})", string.Join(" or ", includeStrings.Select(str => string.Format("{{0}} like '%{0}%'", str.Replace("'", "''")))));
            if (excludeStrings != null)
            {
                template = string.Format("({0})", string.Join(" and ", new[] {template}.Concat(excludeStrings.Select(str => string.Format("{{0}} not like '%{0}%'", str.Replace("'", "''"))))));
            }
            _sqlTemplate = template;

            _includesCache = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        }

        public ActitivityCategory Category
        {
            get { return _category; }
        }

        public string Name
        {
            get { return _name; }
        }

        public bool Includes(string activityName)
        {
            bool value;
            if (!_includesCache.TryGetValue(activityName, out value))
            {
                value = _includeStrings.Any(str => str.Contains("*")
                                                       ? Regex.IsMatch(activityName, str.Replace("*", ".*"), RegexOptions.IgnoreCase)
                                                       : activityName.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0) &&
                        !_excludeStrings.Any(str => str.Contains("*")
                                                        ? Regex.IsMatch(activityName, str.Replace("*", ".*"), RegexOptions.IgnoreCase)
                                                        : activityName.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0);
                _includesCache[activityName] = value;
            }
            return value;
        }

        public string BuildSqlFilter(string column)
        {
            return string.Format(_sqlTemplate, column);
        }
    }

    public enum ActitivityCategory
    {
        Cardio,
        Bodyweight,
        Weights,
        Sports
    }
}