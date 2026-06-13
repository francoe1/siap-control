using System;
using SiapControl.Data;
using SiapControl.Data.Models;
using Xunit;

namespace SiapControl.Tests.Data
{
    public class AutoUpdateSettingsRepositoryTests
    {
        [Fact]
        public void Get_CreatesDefaultSettingsWhenMissing()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new AutoUpdateSettingsRepository(database.Connection);

            AutoUpdateSettingsModel settings = repo.Get();

            Assert.False(settings.AutoUpdateEnabled);
            Assert.Equal(new TimeSpan(20, 0, 0), settings.ScheduledTime);
            Assert.False(settings.StartWithWindows);
            Assert.Null(settings.LastAutoUpdateRun);
        }

        [Fact]
        public void SaveAndGet_PersistsSettings()
        {
            using var database = new RepositoryTestDatabase();
            var repo = new AutoUpdateSettingsRepository(database.Connection);
            var lastRun = new DateTime(2026, 6, 13, 20, 0, 0);

            repo.Save(new AutoUpdateSettingsModel
            {
                AutoUpdateEnabled = true,
                ScheduledDaysMask = 42,
                ScheduledTime = new TimeSpan(21, 30, 0),
                StartWithWindows = true,
                LastAutoUpdateRun = lastRun
            });

            AutoUpdateSettingsModel settings = repo.Get();

            Assert.True(settings.AutoUpdateEnabled);
            Assert.Equal(42, settings.ScheduledDaysMask);
            Assert.Equal(new TimeSpan(21, 30, 0), settings.ScheduledTime);
            Assert.True(settings.StartWithWindows);
            Assert.Equal(lastRun, settings.LastAutoUpdateRun);
        }
    }
}
