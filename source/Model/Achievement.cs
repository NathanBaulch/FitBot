namespace FitBot.Model
{
    public class Achievement
    {
        public long Id { get; set; }
        public long WorkoutId { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public decimal? Distance { get; set; }
        public decimal? Duration { get; set; }
        public decimal? Speed { get; set; }
        public decimal? Repetitions { get; set; }
        public decimal? Weight { get; set; }
    }
}