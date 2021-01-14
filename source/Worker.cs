using System;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FitBot
{
    public class Worker : BackgroundService
    {
        private readonly IUserPullService _userPull;
        private readonly IWorkoutPullService _workoutPull;
        private readonly IAchievementService _achieveService;
        private readonly IAchievementPushService _achievementPush;
        private readonly ILogger<Worker> _logger;

        public Worker(
            IUserPullService userPull,
            IWorkoutPullService workoutPull,
            IAchievementService achieveService,
            IAchievementPushService achievementPush,
            ILogger<Worker> logger)
        {
            _userPull = userPull;
            _workoutPull = workoutPull;
            _achieveService = achieveService;
            _achievementPush = achievementPush;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var start = DateTime.Now;

                foreach (var user in await _userPull.Pull(stoppingToken))
                {
                    var workouts = await _workoutPull.Pull(user, stoppingToken);
                    var achievements = await _achieveService.Process(user, workouts, stoppingToken);
                    await _achievementPush.Push(achievements, stoppingToken);
                    stoppingToken.ThrowIfCancellationRequested();
                }

                _logger.LogInformation("Processing time: " + (DateTime.Now - start));
            }
        }
    }
}