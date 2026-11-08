using System;
using System.IO;

namespace FloatDock.Services
{
    public static class LoggerService
    {
        private static readonly object _lock = new();
        private static readonly string _logDir;
        private static readonly string _logPath;

        static LoggerService()
        {
            _logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FloatDock",
                "logs"
            );
            _logPath = Path.Combine(_logDir, "floatdock.log");

            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
            catch
            {
                // Fallback silently if directory creation fails
            }
        }

        public static void LogInfo(string message) => Log("INFO", message);
        public static void LogWarning(string message) => Log("WARN", message);
        public static void LogError(string message, Exception? ex = null)
        {
            string formatted = ex == null ? message : $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Log("ERROR", formatted);
        }

        private static void Log(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, entry);
                }
            }
            catch
            {
                // Silently avoid throwing within logger to prevent app crashes
            }
        }
    }
}
