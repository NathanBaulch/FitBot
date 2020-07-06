using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IFitocracyService
    {
        long SelfUserId { get; }
        IList<User> GetFollowers(int pageNum = 0);
        IList<Workout> GetWorkouts(long userId, int offset = 0);
        Workout GetWorkout(long workoutId);
        void AddComment(long workoutId, string text);
        void DeleteComment(long commentId);
        void GiveProp(long workoutId);
    }
}