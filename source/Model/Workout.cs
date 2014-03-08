using System;
using System.Collections.Generic;

namespace FitBot.Model
{
    public class Workout
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public int Points { get; set; }
        public long CommentId { get; set; }
        public DateTime ImportDate { get; set; }
        public int Hash { get; set; }
        public IList<Activity> Activities { get; set; }
    }
}