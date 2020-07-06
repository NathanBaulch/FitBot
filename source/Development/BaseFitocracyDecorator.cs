using System.Collections.Generic;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BaseFitocracyDecorator : IFitocracyService
    {
        private readonly IFitocracyService _decorated;

        protected BaseFitocracyDecorator(IFitocracyService decorated)
        {
            _decorated = decorated;
        }

        public long SelfUserId => _decorated.SelfUserId;

        public virtual IList<User> GetFollowers(int pageNum = 0)
        {
            return _decorated.GetFollowers(pageNum);
        }

        public virtual IList<Workout> GetWorkouts(long userId, int offset = 0)
        {
            return _decorated.GetWorkouts(userId, offset);
        }

        public virtual Workout GetWorkout(long workoutId)
        {
            return _decorated.GetWorkout(workoutId);
        }

        public virtual void AddComment(long workoutId, string text)
        {
            _decorated.AddComment(workoutId, text);
        }

        public virtual void DeleteComment(long commentId)
        {
            _decorated.DeleteComment(commentId);
        }

        public virtual void GiveProp(long workoutId)
        {
            _decorated.GiveProp(workoutId);
        }
    }
}