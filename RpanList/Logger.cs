using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpanList
{
    public static class Logger
    {
        public static List<LogEntry> entries = new List<LogEntry>();
        public static event LogEntryAddedEventHandler LogEntryAdded;
        public delegate void LogEntryAddedEventHandler(LogEntry logEntry);

        public static void Log(LogSeverity severity, string content)
        {
            Log(new LogEntry(LogType.AppStarted, severity, content));
        }

        public static void Log(LogEntry entry)
        {
            LogEntryAdded?.Invoke(entry);
            entries.Add(entry);
        }
    }
}
