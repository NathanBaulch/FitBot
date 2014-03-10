namespace FitBot.Model
{
    public class Achievement
    {
        public long Id { get; set; }
        public long WorkoutId { get; set; }
        public string Type { get; set; }
        public string Group { get; set; }
        public double? Quantity1 { get; set; }
        public double? Quantity2 { get; set; }
    }
}