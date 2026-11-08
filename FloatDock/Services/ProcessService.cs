using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using FloatDock.Native;

namespace FloatDock.Services
{
    /// <summary>
    /// Implements Tool 2 (Task Manager Shortcut):
    /// Spawns or brings existing Task Manager to front, handling UAC gracefully.
    /// </summary>
    public static class ProcessService
    {
        private const int ERROR_CANCELLED_BY_USER = 1223; // Win32 UAC cancellation error code

        public static void OpenOrActivateTaskManager()
        {
            try
            {
                // Check if Task Manager is already running
                var processes = Process.GetProcessesByName("Taskmgr");
                if (processes.Length > 0)
                {
                    foreach (var proc in processes)
                    {
                        IntPtr hWnd = proc.MainWindowHandle;
                        if (hWnd != IntPtr.Zero)
                        {
                            LoggerService.LogInfo("Existing Task Manager instance found. Bringing to foreground.");
                            WindowService.ActivateAndForegroundWindow(hWnd);
                            return;
                        }
                    }
                }

                // If not running or main window handle was not yet exposed, launch taskmgr.exe
                string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                string taskmgrPath = Path.Combine(systemDir, "taskmgr.exe");

                var startInfo = new ProcessStartInfo
                {
                    FileName = taskmgrPath,
                    UseShellExecute = true,
                    Verb = "open" // Standard user open; triggers UAC elevation if required by system policy
                };

                LoggerService.LogInfo("Starting new Task Manager process.");
                Process.Start(startInfo);
            }
            catch (Win32Exception win32Ex) when (win32Ex.NativeErrorCode == ERROR_CANCELLED_BY_USER)
            {
                // User clicked "No" or Cancelled the UAC prompt; handle silently per requirements
                LoggerService.LogInfo("Task Manager UAC prompt was cancelled by user.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to launch Task Manager", ex);
            }
        }
    }
}
