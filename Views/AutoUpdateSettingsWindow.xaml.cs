using SiapControl.Common;
using SiapControl.Data;
using SiapControl.Data.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SiapControl.Views
{
    public partial class AutoUpdateSettingsWindow : Window
    {
        private readonly IWindowsStartupService _startupService;
        private readonly CheckBox[] _dayChecks;

        public AutoUpdateSettingsWindow()
            : this(new WindowsStartupService())
        {
        }

        public AutoUpdateSettingsWindow(IWindowsStartupService startupService)
        {
            _startupService = startupService;
            InitializeComponent();
            _dayChecks = new[]
            {
                SundayCheck,
                MondayCheck,
                TuesdayCheck,
                WednesdayCheck,
                ThursdayCheck,
                FridayCheck,
                SaturdayCheck
            };

            Loaded += (_, __) => LoadSettings();
            SaveButton.Click += Save;
            CancelButton.Click += (_, __) => Close();
        }

        private void LoadSettings()
        {
            AutoUpdateSettingsModel settings = Database.AutoUpdateSettings.Get();
            EnabledCheck.IsChecked = settings.AutoUpdateEnabled;
            StartWithWindowsCheck.IsChecked = settings.StartWithWindows || _startupService.IsEnabled();
            TimeText.Text = settings.ScheduledTime.ToString(@"hh\:mm");

            for (int i = 0; i < _dayChecks.Length; i++)
            {
                _dayChecks[i].IsChecked = (settings.ScheduledDaysMask & (1 << i)) != 0;
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            if (!TimeSpan.TryParse(TimeText.Text, out TimeSpan time))
            {
                MessageBox.Show(this, "Ingresa una hora valida con formato HH:mm.", "Hora invalida", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int daysMask = _dayChecks
                .Select((check, index) => check.IsChecked == true ? 1 << index : 0)
                .Aggregate(0, (current, value) => current | value);

            if (daysMask == 0)
            {
                MessageBox.Show(this, "Selecciona al menos un dia.", "Sin dias", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool startWithWindows = StartWithWindowsCheck.IsChecked == true;
            _startupService.SetEnabled(startWithWindows);

            AutoUpdateSettingsModel settings = Database.AutoUpdateSettings.Get();
            settings.AutoUpdateEnabled = EnabledCheck.IsChecked == true;
            settings.StartWithWindows = startWithWindows;
            settings.ScheduledDaysMask = daysMask;
            settings.ScheduledTime = new TimeSpan(time.Hours, time.Minutes, 0);
            Database.AutoUpdateSettings.Save(settings);

            DialogResult = true;
            Close();
        }
    }
}
