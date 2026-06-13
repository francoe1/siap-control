using System;

namespace SiapControl.Data.Models
{
    public class AutoUpdateSettingsModel
    {
        public bool AutoUpdateEnabled { get; set; }
        public int ScheduledDaysMask { get; set; }
        public TimeSpan ScheduledTime { get; set; } = new TimeSpan(20, 0, 0);
        public bool StartWithWindows { get; set; }
        public DateTime? LastAutoUpdateRun { get; set; }
    }
}
