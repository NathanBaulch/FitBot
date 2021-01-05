namespace FitBot.Services
{
    public interface IActivityGroupingService
    {
        string GetActivityGroup(string activityName);
        ActivityCategory? GetGroupCategory(string groupName);
    }

    public enum ActivityCategory
    {
        Cardio,
        Bodyweight,
        Weights,
        Sports
    }
}