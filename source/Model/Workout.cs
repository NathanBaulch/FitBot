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
        public int ActivitiesHash { get; set; }
        public IList<Activity> Activities { get; set; }

        public bool HasChanges(Workout workout)
        {
            return Date != workout.Date ||
                   Points != workout.Points ||
                   ActivitiesHash != workout.ActivitiesHash;
        }
    }
}