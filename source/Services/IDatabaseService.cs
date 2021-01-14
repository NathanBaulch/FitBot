using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IDatabaseService
    {
        Task<IEnumerable<T>> Query<T>(string sql, object parameters = null);
        Task<T> Single<T>(string sql, object parameters = null);

        Task<IEnumerable<User>> GetUsers();
        void Insert(User user);
        void Update(User user);
        void Delete(User user);

        Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<IEnumerable<long>> GetUnresolvedWorkoutIds(long userId, DateTime after);
        void DeleteWorkouts(long userId, DateTime before);
        void Insert(Workout workout);
        void Update(Workout workout, bool deep = false);
        void Delete(Workout workout);

        Task<IEnumerable<Achievement>> GetAchievements(long workoutId);
        void Insert(Achievement achievement);
        void Update(Achievement achievement);
        void UpdateCommentId(long achievementId, long commentId);
        void Delete(Achievement achievement);
    }
}