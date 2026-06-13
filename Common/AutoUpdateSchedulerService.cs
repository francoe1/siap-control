using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiapControl.Common
{
    public interface IAutoUpdateClock
    {
        DateTime Now { get; }
    }

    public sealed class SystemAutoUpdateClock : IAutoUpdateClock
    {
        public DateTime Now => DateTime.Now;
    }

    public sealed class AutoUpdateSchedulerService : IDisposable
    {
        private readonly Func<Task<AutoUpdateRunResult>> _runAsync;
        private readonly IAutoUpdateClock _clock;
        private Timer? _timer;
        private bool _isRunning;

        public event EventHandler<AutoUpdateRunResult>? RunCompleted;
        public event EventHandler<Exception>? RunFailed;

        public AutoUpdateSchedulerService(Func<Task<AutoUpdateRunResult>> runAsync)
            : this(runAsync, new SystemAutoUpdateClock())
        {
        }

        public AutoUpdateSchedulerService(Func<Task<AutoUpdateRunResult>> runAsync, IAutoUpdateClock clock)
        {
            _runAsync = runAsync;
            _clock = clock;
        }

        public void Start()
        {
            _timer ??= new Timer(async _ => await TickAsync(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));
        }

        public async Task RunNowAsync()
        {
            await ExecuteAsync(markLastRun: false);
        }

        public async Task TickAsync()
        {
            AutoUpdateSettingsModel settings = Database.AutoUpdateSettings.Get();
            if (!ShouldRun(settings, _clock.Now))
            {
                return;
            }

            await ExecuteAsync(markLastRun: true);
        }

        public static bool ShouldRun(AutoUpdateSettingsModel settings, DateTime now)
        {
            if (!settings.AutoUpdateEnabled)
            {
                return false;
            }

            int todayMask = 1 << (int)now.DayOfWeek;
            if ((settings.ScheduledDaysMask & todayMask) == 0)
            {
                return false;
            }

            if (now.TimeOfDay < settings.ScheduledTime || now.TimeOfDay >= settings.ScheduledTime.Add(TimeSpan.FromMinutes(1)))
            {
                return false;
            }

            return !settings.LastAutoUpdateRun.HasValue || settings.LastAutoUpdateRun.Value.Date != now.Date;
        }

        private async Task ExecuteAsync(bool markLastRun)
        {
            if (_isRunning)
            {
                return;
            }

            try
            {
                _isRunning = true;
                AutoUpdateRunResult result = await _runAsync();
                if (markLastRun)
                {
                    Database.AutoUpdateSettings.UpdateLastRun(_clock.Now);
                }

                RunCompleted?.Invoke(this, result);
            }
            catch (Exception ex)
            {
                RunFailed?.Invoke(this, ex);
            }
            finally
            {
                _isRunning = false;
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
