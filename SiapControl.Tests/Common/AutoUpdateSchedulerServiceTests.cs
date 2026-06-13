using System;
using SiapControl.Common;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Common
{
    public class AutoUpdateSchedulerServiceTests
    {
        [Fact]
        public void ShouldRun_ReturnsTrueInsideConfiguredMinute()
        {
            var settings = new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = true,
                ScheduledDaysMask = 1 << (int)DayOfWeek.Saturday,
                ScheduledTime = new TimeSpan(20, 15, 0)
            };

            Assert.True(AutoUpdateSchedulerService.ShouldRun(settings, new DateTime(2026, 6, 13, 20, 15, 30)));
        }

        [Fact]
        public void ShouldRun_ReturnsFalseWhenAlreadyRanToday()
        {
            var settings = new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = true,
                ScheduledDaysMask = 1 << (int)DayOfWeek.Saturday,
                ScheduledTime = new TimeSpan(20, 15, 0),
                LastAutoUpdateRun = new DateTime(2026, 6, 13, 20, 15, 10)
            };

            Assert.False(AutoUpdateSchedulerService.ShouldRun(settings, new DateTime(2026, 6, 13, 20, 15, 30)));
        }

        [Fact]
        public void ShouldRun_ReturnsFalseWhenDisabled()
        {
            var settings = new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = false,
                ScheduledDaysMask = 1 << (int)DayOfWeek.Saturday,
                ScheduledTime = new TimeSpan(20, 15, 0)
            };

            Assert.False(AutoUpdateSchedulerService.ShouldRun(settings, new DateTime(2026, 6, 13, 20, 15, 30)));
        }
    }
}
