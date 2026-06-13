using SiapControl.Common;
using SiapControl.Views;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using Forms = System.Windows.Forms;

namespace SiapControl
{
    public partial class App : Application
    {
        private Forms.NotifyIcon? _notifyIcon;
        private AutoUpdateSchedulerService? _scheduler;
        private bool _exitRequested;
        private bool _databaseReady;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            CreateTrayIcon();
            ShowMainWindow();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _scheduler?.Dispose();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            base.OnExit(e);
        }

        public void StartBackgroundServices()
        {
            _databaseReady = true;
            if (_scheduler != null)
            {
                return;
            }

            _scheduler = new AutoUpdateSchedulerService(() => new AutoUpdateRunner().RunAsync());
            _scheduler.RunCompleted += (_, result) => ShowBalloon("Autoupdater", result.Summary, Forms.ToolTipIcon.Info);
            _scheduler.RunFailed += (_, ex) => ShowBalloon("Autoupdater", "No se pudo ejecutar la actualizacion automatica: " + ex.Message, Forms.ToolTipIcon.Warning);
            _scheduler.Start();
        }

        public async Task RunAutoUpdateNowAsync()
        {
            if (!_databaseReady)
            {
                ShowBalloon("Autoupdater", "La base de datos todavia se esta preparando.", Forms.ToolTipIcon.Info);
                return;
            }

            StartBackgroundServices();
            if (_scheduler != null)
            {
                ShowBalloon("Autoupdater", "Buscando actualizaciones de modulos...", Forms.ToolTipIcon.Info);
                await _scheduler.RunNowAsync();
            }
        }

        public void ShowMainWindow()
        {
            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
                MainWindow.Closing += MainWindowClosing;
            }

            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }

        public void ShowSettings()
        {
            ShowMainWindow();
            if (!_databaseReady)
            {
                ShowBalloon("Autoupdater", "La configuracion va a estar disponible cuando termine de abrir la base de datos.", Forms.ToolTipIcon.Info);
                return;
            }

            var window = new AutoUpdateSettingsWindow { Owner = MainWindow };
            window.ShowDialog();
        }

        public void ExitApplication()
        {
            _exitRequested = true;
            Shutdown();
        }

        private void MainWindowClosing(object? sender, CancelEventArgs e)
        {
            if (_exitRequested)
            {
                return;
            }

            e.Cancel = true;
            MainWindow?.Hide();
            ShowBalloon("SiapControl", "SiapControl sigue ejecutandose en segundo plano.", Forms.ToolTipIcon.Info);
        }

        private void CreateTrayIcon()
        {
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Abrir", null, (_, __) => Dispatcher.Invoke(ShowMainWindow));
            menu.Items.Add("Buscar actualizaciones ahora", null, async (_, __) =>
            {
                Task task = await Dispatcher.InvokeAsync(() => RunAutoUpdateNowAsync());
                await task;
            });
            menu.Items.Add("Configurar autoupdater", null, (_, __) => Dispatcher.Invoke(ShowSettings));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Salir", null, (_, __) => Dispatcher.Invoke(ExitApplication));

            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "SiapControl",
                Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location),
                ContextMenuStrip = menu,
                Visible = true
            };
            _notifyIcon.DoubleClick += (_, __) => Dispatcher.Invoke(ShowMainWindow);
        }

        private void ShowBalloon(string title, string text, Forms.ToolTipIcon icon)
        {
            if (_notifyIcon == null)
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = text;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(5000);
        }
    }
}

