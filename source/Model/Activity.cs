using System.Collections.Generic;

namespace FitBot.Model
{
    public class Activity
    {
        public long Id { get; set; }
        public long WorkoutId { get; set; }
        public int Sequence { get; set; }
        public string Name { get; set; }
        public string Note { get; set; }
        public IList<Set> Sets { get; set; }
    }
}