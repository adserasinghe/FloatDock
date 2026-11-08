using System;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using FloatDock.Services;
using FloatDock.Views;

namespace FloatDock
{
    public partial class App : Application
    {
        private const string MutexName = @"Global\FloatDock_SingleInstance_Mutex";
        private Mutex? _singleInstanceMutex;
        private SettingsService? _settingsService;
        private MainWindow? _mainWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure Global Exception Logging before any logic executes
            SetupGlobalExceptionHandling();

            // Enforce single instance via named system Mutex
            _singleInstanceMutex = new Mutex(true, MutexName, out bool isOnlyInstance);
            if (!isOnlyInstance)
            {
                LoggerService.LogWarning("FloatDock is already running. Exiting secondary process instance.");
                MessageBox.Show(
                    "FloatDock is already running.\nCheck your system tray or press Ctrl+Alt+W to reveal the widget.",
                    "FloatDock",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Shutdown();
                return;
            }

            LoggerService.LogInfo("FloatDock starting up. Initializing services.");

            base.OnStartup(e);

            _settingsService = new SettingsService();
            _mainWindow = new MainWindow(_settingsService);
            _mainWindow.Show();
        }

        private void SetupGlobalExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                {
                    LoggerService.LogError("AppDomain Unhandled Exception", ex);
                }
            };

            DispatcherUnhandledException += (s, args) =>
            {
                LoggerService.LogError("WPF Dispatcher Unhandled Exception", args.Exception);
                // Prevent crash if recoverable
                args.Handled = true;
            };

            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, args) =>
            {
                LoggerService.LogError("Unobserved Task Exception", args.Exception);
                args.SetObserved();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggerService.LogInfo("FloatDock shutting down.");
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
