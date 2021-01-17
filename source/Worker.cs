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

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var start = DateTime.Now;
                try
                {
                    Execute(stoppingToken);
                    _logger.LogInformation("Processing time: " + (DateTime.Now - start));
                }
                catch (Exception ex)
                {
                    _logger.LogInformation("Processing time: " + (DateTime.Now - start));

                    if (ex.GetBaseException() is OperationCanceledException)
                    {
                        break;
                    }

                    _logger.LogError(ex.Message + ", retrying in 10 seconds", ex);
                    Task.Delay(TimeSpan.FromSeconds(10), stoppingToken).Wait(stoppingToken);
                }
            }

            return Task.CompletedTask;
        }

        private void Execute(CancellationToken stoppingToken)
        {
            foreach (var user in _userPull.Pull(stoppingToken))
            {
                var workouts = _workoutPull.Pull(user, stoppingToken);
                var achievements = _achieveService.Process(user, workouts, stoppingToken);
                _achievementPush.Push(achievements, stoppingToken);
                stoppingToken.ThrowIfCancellationRequested();
            }
        }
    }
}