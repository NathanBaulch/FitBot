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
        Task<IEnumerable<Workout>> GetWorkouts(long userId, DateTime fromDate, DateTime toDate);
        void Insert(User user);
        void Insert(Workout workout);
        void Update(User user);
        void Update(Workout workout);
        void Delete(User user);
        void Delete(Workout workout);
    }
}