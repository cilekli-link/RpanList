using System;

namespace RpanList
{
    public class LogEntry
    {
        public LogType Type;
        public LogSeverity Severity;
        public DateTime Timestamp;
        public string Content;

        public LogEntry(LogType type, LogSeverity severity, string content)
        {
            Type = type;
            Severity = severity;
            Timestamp = DateTime.Now;
            Content = content;
        }
    }

    public enum LogType
    {
        AppStarted,

        Connected,
        Disconnected,
        ConnectionFailed,

        StreamStarted,
        StreamEnded,

        DownloadStarted,
        DownloadEnded,
    }

    public enum LogSeverity
    {
        Debug,
        Info,
        Warning,
        Error
    }
}