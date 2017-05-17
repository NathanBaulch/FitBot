using System;
using System.Collections.Generic;

namespace FitBot.Model
{
    public class Workout
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public DateTime Date { get; set; }
        public int? Points { get; set; }
        public DateTime InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int ActivitiesHash { get; set; }
        public IList<Activity> Activities { get; set; }
        public WorkoutState State { get; set; }
        public IList<Comment> Comments { get; set; }

        public bool HasChanges(Workout workout)
        {
            return Date != workout.Date ||
                   Points != workout.Points ||
                   ActivitiesHash != workout.ActivitiesHash;
        }
    }

    public enum WorkoutState
    {
        Unchanged,
        Added,
        Updated,
        UpdatedDeep,
        Deleted,
        Unresolved
    }

    public class Comment
    {
        public long Id { get; set; }
        public string Text { get; set; }
    }
}