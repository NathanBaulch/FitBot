using System;
using System.Collections.Generic;
using FitBot.Model;

namespace FitBot.Services
{
    public interface IDatabaseService
    {
        IEnumerable<T> Query<T>(string sql, object parameters = null, int limit = 0);
        T Single<T>(string sql, object parameters = null, bool limit = false);

        IEnumerable<User> GetUsers();
        User GetUser(long id);
        void Insert(User user);
        void Update(User user);
        void Delete(User user);

        IEnumerable<Workout> GetWorkouts(long userId, DateTime? fromDate = null, DateTime? toDate = null);
        IEnumerable<Workout> GetUnprocessedWorkouts(long userId);
        IEnumerable<long> GetUnresolvedWorkoutIds(long userId, DateTime after);
        void DeleteWorkouts(long userId, DateTime before);
        void Insert(Workout workout);
        void Update(Workout workout, bool deep = false);
        void UpdateIsProcessed(long workoutId, bool isProcessed);
        void Delete(Workout workout);

        IEnumerable<Activity> GetActivities(long workoutId);

        IEnumerable<Achievement> GetAchievements(long workoutId);
        IEnumerable<Achievement> GetUnpushedAchievements(long userId, DateTime after);
        void Insert(Achievement achievement);
        void Update(Achievement achievement);
        void UpdateIsPushed(long achievementId, bool isPushed);
        void UpdateCommentId(long achievementId, long commentId);
        void Delete(Achievement achievement);
    }
}