namespace FitBot.Model
{
    public class Set
    {
        public long Id { get; set; }
        public long ActivityId { get; set; }
        public int Sequence { get; set; }
        public int Points { get; set; }
        public decimal? Distance { get; set; }
        public decimal? Duration { get; set; }
        public decimal? Speed { get; set; }
        public decimal? Repetitions { get; set; }
        public decimal? Weight { get; set; }
        public decimal? HeartRate { get; set; }
        public decimal? Incline { get; set; }
        public string Difficulty { get; set; }
        public bool IsPr { get; set; }
    }
}