using System.Collections.Generic;
using System.Threading.Tasks;
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

        public long SelfUserId
        {
            get { return _decorated.SelfUserId; }
        }

        public virtual Task<IList<User>> GetFollowers(int pageNum = 0)
        {
            return _decorated.GetFollowers(pageNum);
        }

        public virtual Task<IList<Workout>> GetWorkouts(long userId, int offset = 0)
        {
            return _decorated.GetWorkouts(userId, offset);
        }

        public virtual Task<IDictionary<long, string>> GetWorkoutComments(long workoutId)
        {
            return _decorated.GetWorkoutComments(workoutId);
        }

        public virtual Task AddComment(long workoutId, string text)
        {
            return _decorated.AddComment(workoutId, text);
        }

        public virtual Task DeleteComment(long commentId)
        {
            return _decorated.DeleteComment(commentId);
        }

        public virtual Task GiveProp(long workoutId)
        {
            return _decorated.GiveProp(workoutId);
        }
    }
}