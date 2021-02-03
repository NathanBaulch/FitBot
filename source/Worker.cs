using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using FitBot.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FitBot
{
    public class Worker : BackgroundService
    {
        private readonly SemaphoreSlim _active = new(1);
        private CancellationTokenSource _suspendCts;

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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemEvents.PowerModeChanged += OnPowerModeChanged;
            }
        }

        public override void Dispose()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            }

            _active.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        [SupportedOSPlatform("windows")]
        private void OnPowerModeChanged(object _, PowerModeChangedEventArgs args)
        {
            switch (args.Mode)
            {
                case PowerModes.Suspend:
                    Suspend();
                    break;
                case PowerModes.Resume:
                    Resume();
                    break;
            }
        }

        private void Suspend()
        {
            _logger.LogInformation("Suspend");
            _active.Wait(1000);
            _suspendCts?.Cancel();
        }

        private void Resume()
        {
            _logger.LogInformation("Resume");
            _active.Release();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _active.WaitAsync(stoppingToken);
                _active.Release();
                using (_suspendCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                {
                    await Execute(_suspendCts.Token);
                }
            }
        }

        private async Task Execute(CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                try
                {
                    var start = DateTime.Now;
                    try
                    {
                        foreach (var user in await _userPull.Pull(cancel))
                        {
                            var workouts = await _workoutPull.Pull(user, cancel);
                            var achievements = _achieveService.Process(user, workouts, cancel);
                            await _achievementPush.Push(achievements, cancel);
                            cancel.ThrowIfCancellationRequested();
                        }
                    }
                    finally
                    {
                        _logger.LogInformation("Processing time: " + (DateTime.Now - start));
                    }
                }
                catch (Exception ex) when (ex.GetBaseException() is OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(10), cancel);
                }
            }
        }
    }
}