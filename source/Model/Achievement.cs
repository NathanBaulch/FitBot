using System;

namespace FitBot.Model
{
    public class Achievement
    {
        public long Id { get; set; }
        public long WorkoutId { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public string Activity { get; set; }
        public decimal? Distance { get; set; }
        public decimal? Duration { get; set; }
        public decimal? Speed { get; set; }
        public decimal? Repetitions { get; set; }
        public decimal? Weight { get; set; }
        public long? CommentId { get; set; }
        public string CommentText { get; set; }
        public bool IsPropped { get; set; }
        public DateTime InsertDate { get; set; }
        public DateTime? UpdateDate { get; set; }
        public bool IsPushed { get; set; }

        public bool HasChanges(Achievement achievement) =>
            Distance != achievement.Distance ||
            Duration != achievement.Duration ||
            Speed != achievement.Speed ||
            Repetitions != achievement.Repetitions ||
            Weight != achievement.Weight ||
            CommentText != achievement.CommentText ||
            IsPropped != achievement.IsPropped;
    }
}