using System.Collections.Generic;
using System.Linq;

namespace FitBot.Services
{
    public class ActivityGroupingService : IActivityGroupingService
    {
        private readonly IDictionary<string, ActivityGroup> _groups = new[]
            {
                Builder.Bodyweight("Burpee").Include("burpee"),
                Builder.Bodyweight("Dip").Include("dip").Exclude("station"),
                Builder.Bodyweight("Lunge").Include("lunge"),
                Builder.Bodyweight("Pull-Up").Include("pull-up", "chin-up"),
                Builder.Bodyweight("Push-Up").Include("push-up"),
                Builder.Bodyweight("Sit-Up").Include("sit-up", "crunch", "bicycle", "v-up"),
                Builder.Cardio("Cycling").Include("cycling", "biking"),
                Builder.Cardio("Rowing").Include("rowing"),
                Builder.Cardio("Running").Include("running").Exclude("stairs"),
                Builder.Cardio("Swimming").Include("swimming"),
                Builder.Cardio("Walking").Include("walking", "hiking").Exclude("lunge"),
                Builder.Sports("American Football").Include("american football"),
                Builder.Sports("Football").Include("soccer"),
                Builder.Sports("Martial Arts").Include("krav maga", "kickboxing", "taekwondo"),
                Builder.Sports("Plank").Include("plank"),
                Builder.Sports("Snowboarding").Include("snowboarding"),
                Builder.Sports("Squash").Include("squash"),
                Builder.Sports("Surfing").Include("surfing"),
                Builder.Sports("Volleyball").Include("volleyball"),
                Builder.Sports("Yoga").Include("yoga", "pilates", "ashtanga").Exclude("push-up"),
                Builder.Weights("Bench Press").Include("bench press", "floor press", "chest press", "triceps press"),
                Builder.Weights("Bicep Curl").Include("bicep curl", "dumbbell curl", "hammer curl", "barbell curl", "preacher curl", "concentration curl"),
                Builder.Weights("Clean").Include("clean").Exclude("squat"),
                Builder.Weights("Deadlift").Include("deadlift"),
                Builder.Weights("Flyes").Include("flyes", "chest fly"),
                Builder.Weights("Good Morning").Include("good morning"),
                Builder.Weights("Kettlebell Swing").Include("kettlebell swing"),
                Builder.Weights("Kickback").Include("kickback"),
                Builder.Weights("Lateral Raise").Include("lateral raise"),
                Builder.Weights("Leg Curl").Include("leg curl"),
                Builder.Weights("Leg Press").Include("leg press"),
                Builder.Weights("Pulldown").Include("pulldown"),
                Builder.Weights("Pullover").Include("pullover"),
                Builder.Weights("Pushdown").Include("pushdown"),
                Builder.Weights("Row").Include(" row"),
                Builder.Weights("Shoulder Press").Include("shoulder press", "military", "kettlebell press"),
                Builder.Weights("Shrug").Include("shrug"),
                Builder.Weights("Squat").Include("squat"),
                Builder.Weights("Tricep Extension").Include("tricep extension", "triceps extension"),
            }.Select(builder => builder.Build())
             .ToDictionary(group => group.Name);

        public IEnumerable<ActivityGroup> GetAll()
        {
            return _groups.Values;
        }

        public ActivityGroup Get(string name)
        {
            return _groups[name];
        }

        #region Nested type: Builder

        private class Builder
        {
            private readonly string _name;
            private readonly ActitivityCategory _category;
            private IList<string> _includeStrings;
            private IList<string> _excludeStrings;

            public static Builder Cardio(string name)
            {
                return new Builder(ActitivityCategory.Cardio, name);
            }

            public static Builder Bodyweight(string name)
            {
                return new Builder(ActitivityCategory.Bodyweight, name);
            }

            public static Builder Weights(string name)
            {
                return new Builder(ActitivityCategory.Weights, name);
            }

            public static Builder Sports(string name)
            {
                return new Builder(ActitivityCategory.Sports, name);
            }

            private Builder(ActitivityCategory category, string name)
            {
                _category = category;
                _name = name;
            }

            public Builder Include(params string[] strings)
            {
                _includeStrings = strings;
                return this;
            }

            public Builder Exclude(params string[] strings)
            {
                _excludeStrings = strings;
                return this;
            }

            public ActivityGroup Build()
            {
                return new ActivityGroup(_category, _name, _includeStrings, _excludeStrings);
            }
        }

        #endregion
    }
}