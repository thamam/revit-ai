using System;
using System.Collections.Generic;
using RevitAI.Services;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock implementation of LoggingService for integration testing.
    /// Captures all log entries for verification in tests.
    /// Note: Cannot inherit from LoggingService (private constructor), so we create a standalone mock.
    /// </summary>
    public class MockLoggingService
    {
        public List<LogEntry> Logs { get; } = new List<LogEntry>();

        public List<string> InfoLogs => GetLogsByLevel("INFO");
        public List<string> DebugLogs => GetLogsByLevel("DEBUG");
        public List<string> WarningLogs => GetLogsByLevel("WARNING");
        public List<string> ErrorLogs => GetLogsByLevel("ERROR");

        public void Info(string message, string context = null)
        {
            Logs.Add(new LogEntry("INFO", message, context));
        }

        public void Warning(string message, string context = null)
        {
            Logs.Add(new LogEntry("WARNING", message, context));
        }

        public void Error(string message, string context = null, Exception exception = null)
        {
            string fullMessage = message;
            if (exception != null)
            {
                fullMessage += $"\nException: {exception.Message}";
            }
            Logs.Add(new LogEntry("ERROR", fullMessage, context));
        }

        public void Debug(string message, string context = null)
        {
            Logs.Add(new LogEntry("DEBUG", message, context));
        }

        /// <summary>
        /// Get all logs with a specific level.
        /// </summary>
        private List<string> GetLogsByLevel(string level)
        {
            var messages = new List<string>();
            foreach (var log in Logs)
            {
                if (log.Level == level)
                {
                    messages.Add(log.Message);
                }
            }
            return messages;
        }

        /// <summary>
        /// Get all logs with a specific context.
        /// </summary>
        public List<string> GetLogsByContext(string context)
        {
            var messages = new List<string>();
            foreach (var log in Logs)
            {
                if (log.Context == context)
                {
                    messages.Add(log.Message);
                }
            }
            return messages;
        }

        /// <summary>
        /// Check if any log contains a specific substring.
        /// </summary>
        public bool ContainsLogWith(string substring)
        {
            foreach (var log in Logs)
            {
                if (log.Message.Contains(substring, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clear all logged entries.
        /// </summary>
        public void Clear()
        {
            Logs.Clear();
        }
    }

    /// <summary>
    /// Represents a single log entry for testing.
    /// </summary>
    public class LogEntry
    {
        public string Level { get; set; }
        public string Message { get; set; }
        public string Context { get; set; }
        public DateTime Timestamp { get; set; }

        public LogEntry(string level, string message, string context)
        {
            Level = level;
            Message = message;
            Context = context;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Level}] {Context}: {Message}";
        }
    }
}
