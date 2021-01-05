using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BaseFitocracyDecorator : IFitocracyService
    {
        private readonly IFitocracyService _decorated;

        protected BaseFitocracyDecorator(IFitocracyService decorated) => _decorated = decorated;

        public long SelfUserId => _decorated.SelfUserId;

        public virtual Task<IList<User>> GetFollowers(int pageNum) => _decorated.GetFollowers(pageNum);

        public virtual Task<IList<Workout>> GetWorkouts(long userId, int offset) => _decorated.GetWorkouts(userId, offset);

        public virtual Task<Workout> GetWorkout(long workoutId) => _decorated.GetWorkout(workoutId);

        public virtual Task AddComment(long workoutId, string text) => _decorated.AddComment(workoutId, text);

        public virtual Task DeleteComment(long commentId) => _decorated.DeleteComment(commentId);

        public virtual Task GiveProp(long workoutId) => _decorated.GiveProp(workoutId);
    }
}