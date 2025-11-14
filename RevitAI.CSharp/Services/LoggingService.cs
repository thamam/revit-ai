using System;
using System.IO;

namespace RevitAI.Services
{
    /// <summary>
    /// Logging Service
    /// Provides structured logging to file for debugging and diagnostics
    /// Story 1.6: Logging & Diagnostics Infrastructure
    /// </summary>
    public class LoggingService
    {
        private static LoggingService _instance;
        private static readonly object _lock = new object();
        private readonly string _logFilePath;
        private readonly long _maxFileSizeBytes = 10 * 1024 * 1024; // 10MB
        private readonly int _maxBackupFiles = 5;

        private LoggingService()
        {
            // Create log directory in %APPDATA%/RevitAI/logs/
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logDirectory = Path.Combine(appDataPath, "RevitAI", "logs");
            Directory.CreateDirectory(logDirectory);

            _logFilePath = Path.Combine(logDirectory, "revit_ai.log");
        }

        /// <summary>
        /// Get singleton instance
        /// </summary>
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new LoggingService();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Log information message
        /// </summary>
        public void Info(string message, string context = null)
        {
            WriteLog("INFO", message, context);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public void Warning(string message, string context = null)
        {
            WriteLog("WARNING", message, context);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public void Error(string message, string context = null, Exception exception = null)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception.Message}\nStack Trace: {exception.StackTrace}";
            }
            WriteLog("ERROR", fullMessage, context);
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public void Debug(string message, string context = null)
        {
            WriteLog("DEBUG", message, context);
        }

        /// <summary>
        /// Log operation with context
        /// </summary>
        public void LogOperation(string operation, string status, string details = null)
        {
            string message = $"Operation: {operation} | Status: {status}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $" | Details: {details}";
            }
            WriteLog("INFO", message, "OPERATION");
        }

        /// <summary>
        /// Write log entry to file
        /// </summary>
        private void WriteLog(string level, string message, string context)
        {
            lock (_lock)
            {
                try
                {
                    // Check if rotation needed
                    RotateLogFileIfNeeded();

                    // Format log entry
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string contextPart = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
                    string logEntry = $"{timestamp} [{level}] {contextPart}{message}";

                    // Append to log file
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback: write to console if file logging fails
                    Console.WriteLine($"Logging failed: {ex.Message}");
                    Console.WriteLine($"Original log: [{level}] {message}");
                }
            }
        }

        /// <summary>
        /// Rotate log file if it exceeds size limit
        /// </summary>
        private void RotateLogFileIfNeeded()
        {
            if (!File.Exists(_logFilePath))
                return;

            var fileInfo = new FileInfo(_logFilePath);
            if (fileInfo.Length < _maxFileSizeBytes)
                return;

            // Rotate existing backups
            for (int i = _maxBackupFiles - 1; i >= 1; i--)
            {
                string oldBackup = $"{_logFilePath}.{i}";
                string newBackup = $"{_logFilePath}.{i + 1}";

                if (File.Exists(oldBackup))
                {
                    if (i == _maxBackupFiles - 1)
                    {
                        // Delete oldest backup
                        File.Delete(oldBackup);
                    }
                    else
                    {
                        // Rename to next number
                        if (File.Exists(newBackup))
                            File.Delete(newBackup);
                        File.Move(oldBackup, newBackup);
                    }
                }
            }

            // Move current log to .1 backup
            string firstBackup = $"{_logFilePath}.1";
            if (File.Exists(firstBackup))
                File.Delete(firstBackup);
            File.Move(_logFilePath, firstBackup);
        }

        /// <summary>
        /// Get current log file path
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }

        /// <summary>
        /// Clear all log files
        /// </summary>
        public void ClearLogs()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_logFilePath))
                        File.Delete(_logFilePath);

                    for (int i = 1; i <= _maxBackupFiles; i++)
                    {
                        string backup = $"{_logFilePath}.{i}";
                        if (File.Exists(backup))
                            File.Delete(backup);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to clear logs: {ex.Message}");
                }
            }
        }
    }
}
