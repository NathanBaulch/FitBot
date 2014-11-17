using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FitBot.Services
{
    public class ActivityGroupingService : IActivityGroupingService
    {
        private readonly IDictionary<string, Group> _groups = new[]
            {
                Builder.Bodyweight("Ab Wheel").Include("ab wheel"),
                Builder.Bodyweight("Band Pull Apart").Include("band pull apart"),
                Builder.Bodyweight("Bicep Clutch").Include("biceps clutch"),
                Builder.Bodyweight("Bird-Dog/Dead Bug").Include("bird-dog", "dead bug"),
                Builder.Bodyweight("Blackbird").Include("blackbird"),
                Builder.Bodyweight("Body Weight Squat").Include("body weight squat", "body weight * squat", "raptor squat", "sissy squat", "jump squat", "squat to stand", "let me ins"),
                Builder.Bodyweight("Bridge").Include("bridge"),
                Builder.Bodyweight("Burpee").Include("burpee"),
                Builder.Bodyweight("Dip").Include("dip").Exclude("station"),
                Builder.Bodyweight("Dragon Flag").Include("dragon flag"),
                Builder.Bodyweight("Fallout").Include("fallout"),
                Builder.Bodyweight("Fire Hydrants").Include("fire hydrants"),
                Builder.Bodyweight("Glute March").Include("glute march"),
                Builder.Bodyweight("Groiner").Include("groiner"),
                Builder.Bodyweight("Hand Gripper").Include("hand gripper"),
                Builder.Bodyweight("Hand Switches").Include("hand switches"),
                Builder.Bodyweight("High Knees").Include("high knees", "butt kickers"),
                Builder.Bodyweight("Hip Adductor/Abductor").Include("duct"),
                Builder.Bodyweight("Hip Extension").Include("hip extension"),
                Builder.Bodyweight("Hip Hinge").Include("hip hinge"),
                Builder.Bodyweight("Hip Raise").Include("hip raise"),
                Builder.Bodyweight("Hip Thrust").Include("hip thrust"),
                Builder.Bodyweight("Hollow Rock").Include("hollow rock"),
                Builder.Bodyweight("Hyperextension").Include("hyperextension", "back extension"),
                Builder.Bodyweight("Inch Worm").Include("inch worm"),
                Builder.Bodyweight("Inverted Row").Include("inverted row", "ring row", "horizontal pull"),
                Builder.Bodyweight("Iron Cross").Include("iron cross"),
                Builder.Bodyweight("Jacks").Include("jacks"),
                Builder.Bodyweight("Jumps").Include("jump").Exclude("squat", "lunge", "rope", "jacks"),
                Builder.Bodyweight("Kicks").Include("flutter kick", "donkey kick"),
                Builder.Bodyweight("Knee Tuck").Include("knee tuck"),
                Builder.Bodyweight("Leg Raise").Include("leg raise", "knee raise", "hamstring raise", "frog raise", "toes-to-bar", "knees-to-elbows"),
                Builder.Bodyweight("Lunge").Include("lunge"),
                Builder.Bodyweight("Mountain Climbers").Include("mountain climbers"),
                Builder.Bodyweight("Muscle-Up").Include("muscle-up"),
                Builder.Bodyweight("Other Bodyweight").Include("other bodyweight"),
                Builder.Bodyweight("Pull-Up").Include("pull-up", "chin-up", "jackknife pull", "mixed grip chin", "vertical pull", "twist of faith"),
                Builder.Bodyweight("Pulse Up").Include("pulse up"),
                Builder.Bodyweight("Push-Up").Include("push-up", "push up", "shove offs").Exclude("hold"),
                Builder.Bodyweight("Reverse Push-Up").Include("reverse pushup"),
                Builder.Bodyweight("Russian Twist").Include("russian twist"),
                Builder.Bodyweight("Scissors").Include("scissor", "clam shell"),
                Builder.Bodyweight("Shoulder Dislocation").Include("shoulder dislocation"),
                Builder.Bodyweight("Side Plank Lift").Include("side plank lift"),
                Builder.Bodyweight("Sit-Up").Include("sit-up", "sit up", "crunch", "bicycle", "v-up").Exclude("scissor"),
                Builder.Bodyweight("Slideboard").Include("slideboard").Exclude("lunge", "curl"),
                Builder.Bodyweight("Step Up").Include("step up"),
                Builder.Bodyweight("Stir The Pot").Include("stir the pot"),
                Builder.Bodyweight("Stretch").Include("flexors stretch", "deltoid stretch", "toe touches"),
                Builder.Bodyweight("Surf Getup").Include("surf getup"),
                Builder.Bodyweight("Walking Stairs").Include("walked up the stairs"),
                Builder.Bodyweight("Walkout").Include("walkout"),
                Builder.Bodyweight("YTI Raises").Include("yti raises"),
                Builder.Bodyweight("Ys").Include("suspension trainer ys"),
                Builder.Cardio("Cycling").Include("cycling", "biking", "spinning"),
                Builder.Cardio("Farmer's Walk").Include("farmer's walk", "crosswalk", "carry", "waiter walk"),
                Builder.Cardio("Hashing").Include("hashing"),
                Builder.Cardio("Rock Climbing").Include("rock climbing"),
                Builder.Cardio("Rope Climb").Include("rope climb"),
                Builder.Cardio("Rowing").Include("rowing"),
                Builder.Cardio("Running").Include("running", "jogging").Exclude("stairs"),
                Builder.Cardio("Swimming").Include("swimming"),
                Builder.Cardio("Walking").Include("walking", "hiking", "snowshoeing").Exclude("lunge", "bridge"),
                Builder.Cardio("Wheelchair Racing").Include("wheelchair racing"),
                Builder.Sports("Adventure Racing").Include("adventure racing", "tough mudder", "spartan race", "warrior dash"),
                Builder.Sports("American Football").Include("american football"),
                Builder.Sports("Aqua Aerobics").Include("aqua aerobics"),
                Builder.Sports("Archery").Include("archery"),
                Builder.Sports("Badminton").Include("badminton"),
                Builder.Sports("Baseball").Include("baseball"),
                Builder.Sports("Basketball").Include("basketball"),
                Builder.Sports("Battle Ropes").Include("battle ropes"),
                Builder.Sports("Belly Boarding").Include("belly boarding"),
                Builder.Sports("Boot Camp").Include("boot camp"),
                Builder.Sports("Boxing").Include("boxing").Exclude("kickboxing"),
                Builder.Sports("Broomball").Include("broomball"),
                Builder.Sports("Class").Include("class", "30-day shred"),
                Builder.Sports("Curling").Include("curling"),
                Builder.Sports("Dancing").Include("dancing", "ballet", "crumping", "lindy hopping", "melbourne shuffle", "popping & locking", "salsa", "tango", "tectonic", "zumba"),
                Builder.Sports("Dodgeball").Include("dodgeball"),
                Builder.Sports("Dragon Boating").Include("dragon boating"),
                Builder.Sports("Drumming").Include("drumming"),
                Builder.Sports("Elliptical Trainer").Include("elliptical trainer"),
                Builder.Sports("Fencing").Include("fencing"),
                Builder.Sports("Flexed-Arm Hang").Include("flexed-arm hang"),
                Builder.Sports("Flutter").Include("flutter").Exclude("kicks"),
                Builder.Sports("Foam Rolling").Include("foam rolling"),
                Builder.Sports("Football").Include("soccer"),
                Builder.Sports("Frisbee").Include("frisbee"),
                Builder.Sports("General Program").Include("general").Exclude("yoga", "gymnastics"),
                Builder.Sports("Golf").Include("golf"),
                Builder.Sports("Gymnastics").Include("gymnastics"),
                Builder.Sports("Handstand/Headstand").Include("handstand", "headstand", "crow stand", "frog stand").Exclude("push-up"),
                Builder.Sports("Heavy Bag").Include("heavy bag"),
                Builder.Sports("Hill Training").Include("hill training"),
                Builder.Sports("Hockey").Include("hockey"),
                Builder.Sports("Horseback Riding").Include("horseback riding"),
                Builder.Sports("Hula Hooping").Include("hula hooping"),
                Builder.Sports("Jump Rope").Include("jump rope"),
                Builder.Sports("Kayaking").Include("kayaking"),
                Builder.Sports("L-Sit").Include("l-sit").Exclude("pull-up", "chin-up"),
                Builder.Sports("Lacrosse").Include("lacrosse"),
                Builder.Sports("Ladder Drills").Include("ladder drills"),
                Builder.Sports("Lake Canoeing").Include("lake canoeing"),
                Builder.Sports("Martial Arts").Include("aikido", "brazilian jiu-jitsu", "capoeira", "chin na", "eskrima", "hapkido", "iaido", "jiu-jitsu", "judo", "karate", "kempo", "kendo", "kickboxing", "krav maga", "kung fu", "muay thai", "ninjutsu", "savate", "shuai jiao", "taekwondo", "tai chi", "viet vo dao", "wing chun"),
                Builder.Sports("Meditation").Include("meditation"),
                Builder.Sports("Moving boxes").Include("moving boxes"),
                Builder.Sports("Netball").Include("netball"),
                Builder.Sports("Other Cardio").Include("other cardio"),
                Builder.Sports("Paddleboarding").Include("paddleboarding"),
                Builder.Sports("Paintball").Include("paintball"),
                Builder.Sports("Ping Pong").Include("ping pong"),
                Builder.Sports("Planche").Include("planche").Exclude("push-up"),
                Builder.Sports("Plank").Include("plank", "push-up hold").Exclude("jacks", "lifts", "hip extension"),
                Builder.Sports("Racquetball").Include("racquetball"),
                Builder.Sports("Roller Derby").Include("roller derby"),
                Builder.Sports("Rugby").Include("rugby"),
                Builder.Sports("Running Stairs").Include("running stairs"),
                Builder.Sports("SCA Heavy Combat").Include("sca heavy combat"),
                Builder.Sports("Sailing").Include("sailing"),
                Builder.Sports("Scuba Diving").Include("scuba diving"),
                Builder.Sports("Shoveling Snow").Include("shoveling snow"),
                Builder.Sports("Skateboarding").Include("skateboarding"),
                Builder.Sports("Skating").Include("skating"),
                Builder.Sports("Skiing").Include("skiing", "ski machine"),
                Builder.Sports("Snowboarding").Include("snowboarding"),
                Builder.Sports("Softball").Include("softball"),
                Builder.Sports("Speed Bag").Include("speed bag"),
                Builder.Sports("Squash").Include("squash"),
                Builder.Sports("Stair Machine").Include("stair machine"),
                Builder.Sports("Static Wall Sit").Include("static wall sit"),
                Builder.Sports("Stretching").Include("stretch").Exclude("flexors", "deltoid"),
                Builder.Sports("Superman").Include("superman"),
                Builder.Sports("Surfing").Include("surfing"),
                Builder.Sports("Tennis").Include("tennis"),
                Builder.Sports("Trampoline").Include("trampoline"),
                Builder.Sports("Volleyball").Include("volleyball"),
                Builder.Sports("Wakeboarding").Include("wakeboarding"),
                Builder.Sports("Water Polo").Include("water polo"),
                Builder.Sports("Wrestling").Include("wrestling"),
                Builder.Sports("Yard Work").Include("yard work"),
                Builder.Sports("Yoga").Include("yoga", "ashtanga", "hatha", "pilates", "qigong", "vinyasa").Exclude("push-up"),
                Builder.Weights("Anti-Rotation Chop").Include("rotation chop"),
                Builder.Weights("Around The World").Include("around the world"),
                Builder.Weights("Atlas Stones").Include("atlas stones"),
                Builder.Weights("Ball Slam").Include("ball slam", "wall ball"),
                Builder.Weights("Bench Press").Include("bench press", "floor press", "chest press", "triceps press", "landmine press", "pin press", "squeeze press", "ball press", "prayer press", "incline press", "decline press", "loaded press"),
                Builder.Weights("Bent Press").Include("bent press", "windmill"),
                Builder.Weights("Bicep Curl").Include("bicep curl", "biceps curl", "dumbbell curl", "hammer curl", "barbell curl", "preacher curl", "concentration curl", "cable curl", "bar curl", "reverse curl", "incline curl", "spider curl"),
                Builder.Weights("Cable Crossover").Include("cable crossover"),
                Builder.Weights("Cable Pull Through").Include("cable pull through"),
                Builder.Weights("Calf Extension").Include("calf extension"),
                Builder.Weights("Calf Raise").Include("calf raise"),
                Builder.Weights("Clean").Include("clean").Exclude("squat"),
                Builder.Weights("Deadlift").Include("deadlift").Exclude("lunge"),
                Builder.Weights("Floor Wiper").Include("floor wiper"),
                Builder.Weights("Flyes").Include("flyes", "chest fly"),
                Builder.Weights("Front Raise").Include("front * raise").Exclude("leg"),
                Builder.Weights("Good Morning").Include("good morning"),
                Builder.Weights("High Pull").Include("high pull"),
                Builder.Weights("Jerk").Include("jerk").Exclude("clean", "squat"),
                Builder.Weights("Kettlebell Figure 8").Include("kettlebell figure 8"),
                Builder.Weights("Kettlebell Halo").Include("kettlebell halo"),
                Builder.Weights("Kettlebell Swing").Include("kettlebell swing", "kettlebell pirate ships"),
                Builder.Weights("Kickback").Include("kickback"),
                Builder.Weights("Kneeling Cable Lift").Include("kneeling cable lift"),
                Builder.Weights("Landmines").Include("landmines"),
                Builder.Weights("Lateral Raise").Include("lateral raise", "dumbbell raise", "side lateral").Exclude("front"),
                Builder.Weights("Leg Curl").Include("leg curl", "hamstring curl"),
                Builder.Weights("Leg Extension").Include("leg extension").Exclude("crunch"),
                Builder.Weights("Leg Press").Include("leg press"),
                Builder.Weights("Long Cycle").Include("long cycle"),
                Builder.Weights("Lying Pronation/Supination").Include("lying pronation", "lying supination"),
                Builder.Weights("Neck Extension").Include("neck extension"),
                Builder.Weights("Neck Flexion").Include("flexion"),
                Builder.Weights("Other Weightlifting").Include("other weightlifting"),
                Builder.Weights("Pallof Press").Include("pallof press"),
                Builder.Weights("Plate Pinch").Include("plate pinch"),
                Builder.Weights("Pulldown").Include("pulldown"),
                Builder.Weights("Pullover").Include("pullover"),
                Builder.Weights("Push Press").Include("push press"),
                Builder.Weights("Pushdown").Include("pushdown"),
                Builder.Weights("Rear Delt Raise").Include("delt raise", "deltoid raise", "delt fly", "backhand").Exclude("front"),
                Builder.Weights("Rear Delt Row").Include("delt row", "face pull", "bench pull"),
                Builder.Weights("Row").Include("row").Exclude("crow", "narrow", "rowing", "inverted", "ring", "delt"),
                Builder.Weights("Shoulder Press").Include("shoulder press", "military", "kettlebell press", "dumbbell press", "barbell press", "cuban press", "arnold press", "seated press", "seesaw press", "sots press", "viking press"),
                Builder.Weights("Shoulder Raise").Include("shoulder raise"),
                Builder.Weights("Shoulder Rotation").Include("rotation").Exclude("chop"),
                Builder.Weights("Shrug").Include("shrug"),
                Builder.Weights("Side Bend").Include("side bend"),
                Builder.Weights("Sled").Include("sled"),
                Builder.Weights("Snatch").Include("snatch").Exclude("deadlift"),
                Builder.Weights("Squat").Include("squat", "pistol", "full zercher").Exclude("body weight", "raptor", "sissy", "jump", "to stand", "stretch"),
                Builder.Weights("Thruster").Include("thruster"),
                Builder.Weights("Tire Flip").Include("tire flip"),
                Builder.Weights("Trap Raise").Include("trap raise"),
                Builder.Weights("Tricep Extension").Include("tricep extension", "triceps extension", "skull crusher"),
                Builder.Weights("Turkish Get-Up").Include("turkish get-up"),
                Builder.Weights("Woodchopper").Include("woodchopper", "cable twist"),
                Builder.Weights("Wrist Curl").Include("wrist curl")
            }.Select(builder => builder.Build())
             .ToDictionary(group => group.Name);

        private readonly IDictionary<string, string> _activitiesCache = new ConcurrentDictionary<string, string>();

        public string GetActvityGroup(string activityName)
        {
            string groupName;
            if (!_activitiesCache.TryGetValue(activityName, out groupName))
            {
                var groups = _groups.Values.Where(group => group.Includes(activityName)).ToList();
                if (groups.Count > 0)
                {
                    if (groups.Count > 1)
                    {
                        //TODO: warn
                    }
                    groupName = groups[0].Name;
                }
                _activitiesCache[activityName] = groupName;
            }

            return groupName;
        }

        public ActivityCategory? GetGroupCategory(string groupName)
        {
            Group group;
            return _groups.TryGetValue(groupName, out group)
                       ? group.Category
                       : (ActivityCategory?) null;
        }

        #region Nested type: Builder

        private class Builder
        {
            private readonly string _name;
            private readonly ActivityCategory _category;
            private IList<string> _includeStrings;
            private IList<string> _excludeStrings;

            public static Builder Cardio(string name)
            {
                return new Builder(ActivityCategory.Cardio, name);
            }

            public static Builder Bodyweight(string name)
            {
                return new Builder(ActivityCategory.Bodyweight, name);
            }

            public static Builder Weights(string name)
            {
                return new Builder(ActivityCategory.Weights, name);
            }

            public static Builder Sports(string name)
            {
                return new Builder(ActivityCategory.Sports, name);
            }

            private Builder(ActivityCategory category, string name)
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

            public Group Build()
            {
                return new Group(_category, _name, _includeStrings, _excludeStrings);
            }
        }

        #endregion

        #region Nested type: Group

        private class Group
        {
            private readonly ActivityCategory _category;
            private readonly string _name;
            private readonly IList<string> _includeStrings;
            private readonly IList<string> _excludeStrings;

            public Group(ActivityCategory category, string name, IList<string> includeStrings, IList<string> excludeStrings)
            {
                _category = category;
                _name = name;
                _includeStrings = includeStrings;
                _excludeStrings = excludeStrings ?? new string[0];
            }

            public ActivityCategory Category
            {
                get { return _category; }
            }

            public string Name
            {
                get { return _name; }
            }

            public bool Includes(string activityName)
            {
                return Matches(activityName, _includeStrings) && !Matches(activityName, _excludeStrings);
            }

            private static bool Matches(string name, IEnumerable<string> strings)
            {
                return strings.Any(str => str.Contains("*")
                                              ? Regex.IsMatch(name, str.Replace("*", ".*"), RegexOptions.IgnoreCase)
                                              : name.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0);
            }
        }

        #endregion
    }
}