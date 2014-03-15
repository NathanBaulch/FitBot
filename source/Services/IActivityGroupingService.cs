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
        private readonly Regex _regex;
        private readonly string _sqlTemplate;

        public ActivityGroup(ActitivityCategory category, string name, IList<string> includeStrings, IList<string> excludeStrings = null)
        {
            _category = category;
            _name = name;

            var pattern = string.Format("(?=.*({0}))", string.Join("|", includeStrings));
            if (excludeStrings != null)
            {
                pattern += string.Format("(?!.*({0}))", string.Join("|", excludeStrings));
            }
            _regex = new Regex(pattern, RegexOptions.IgnoreCase);

            var template = string.Format("({0})", string.Join(" or ", includeStrings.Select(str => string.Format("{{0}} like '%{0}%'", str))));
            if (excludeStrings != null)
            {
                template = string.Format("({0})", string.Join(" and ", new[] {template}.Concat(excludeStrings.Select(str => string.Format("{{0}} not like '%{0}%'", str)))));
            }
            _sqlTemplate = template;
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
            return _regex.IsMatch(activityName);
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