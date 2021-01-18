using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Model;
using FitBot.Services;

namespace FitBot.Development
{
    public class BaseFitocracyDecorator : IFitocracyService
    {
        private readonly IFitocracyService _decorated;

        protected BaseFitocracyDecorator(IFitocracyService decorated) => _decorated = decorated;

        public Task<long> GetSelfUserId(CancellationToken cancel) => _decorated.GetSelfUserId(cancel);

        public virtual Task<IList<User>> GetFollowers(int pageNum, CancellationToken cancel) => _decorated.GetFollowers(pageNum, cancel);

        public virtual Task<IList<Workout>> GetWorkouts(long userId, int offset, CancellationToken cancel) => _decorated.GetWorkouts(userId, offset, cancel);

        public virtual Task<Workout> GetWorkout(long workoutId, CancellationToken cancel) => _decorated.GetWorkout(workoutId, cancel);

        public virtual Task AddComment(long workoutId, string text, CancellationToken cancel) => _decorated.AddComment(workoutId, text, cancel);

        public virtual Task DeleteComment(long commentId, CancellationToken cancel) => _decorated.DeleteComment(commentId, cancel);

        public virtual Task GiveProp(long workoutId, CancellationToken cancel) => _decorated.GiveProp(workoutId, cancel);
    }
}