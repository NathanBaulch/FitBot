namespace FitBot.Model
{
    public class Set
    {
        public long Id { get; set; }
        public long ActivityId { get; set; }
        public int Sequence { get; set; }
        public int Points { get; set; }
        public double? Distance { get; set; }
        public int? Duration { get; set; }
        public double? Speed { get; set; }
        public int? Repetitions { get; set; }
        public double? Weight { get; set; }
        public double? HeartRate { get; set; }
        public double? Incline { get; set; }
        public string Difficulty { get; set; }
        public bool IsPb { get; set; }
    }
}