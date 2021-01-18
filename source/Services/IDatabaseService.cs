using System;
using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IDatabaseService
    {
        IEnumerable<T> Query<T>(string sql, object parameters = null);
        T Single<T>(string sql, object parameters = null);

        IEnumerable<User> GetUsers();
        void Insert(User user);
        void Update(User user);
        void Delete(User user);

        IEnumerable<Workout> GetWorkouts(long userId, DateTime? fromDate = null, DateTime? toDate = null);
        IEnumerable<long> GetUnresolvedWorkoutIds(long userId, DateTime after);
        void DeleteWorkouts(long userId, DateTime before);
        void Insert(Workout workout);
        void Update(Workout workout, bool deep = false);
        void Delete(Workout workout);

        IEnumerable<Activity> GetActivities(long workoutId);

        IEnumerable<Achievement> GetAchievements(long workoutId);
        void Insert(Achievement achievement);
        void Update(Achievement achievement);
        void UpdateCommentId(long achievementId, long commentId);
        void Delete(Achievement achievement);
    }
}