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
        Task<IEnumerable<User>> GetUsersWithDirtyDate();
        void Insert(User user);
        void Update(User user);
        void Delete(User user);

        Task<int> GetWorkoutCount(long userId);
        Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime fromDate, DateTime toDate, bool deep = false);
        void DeleteWorkoutsBefore(DateTime date);
        void Insert(Workout workout, bool deep = false);
        void Update(Workout workout, bool deep = false);
        void Delete(Workout workout);

        void Insert(Achievement achievement);
        void Update(Achievement achievement);
        void Delete(Achievement achievement);
    }
}