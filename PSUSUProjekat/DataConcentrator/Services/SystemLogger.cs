using System;
using System.IO;

namespace DataConcentrator
{
    [Flags]
    public enum TraceFlags
    {
        None         = 0,
        Login        = 1,
        AlarmAck     = 2,
        TagAdd       = 4,
        TagUpdate    = 8,
        ImportExport = 16,
        Error        = 32,
        AlarmRaised  = 64,
        ScanChange   = 128,
        WriteToTag   = 256,
        All          = 511
    }

    public static class SystemLogger
    {
        private static readonly object _fileLock = new object();
        private static string _logPath = "system.log";
        private static string _traceWordPath = "traceword.cfg";

        private static TraceFlags _activeFlags;

        public static TraceFlags ActiveFlags
        {
            get { return _activeFlags; }
            set
            {
                _activeFlags = value;
                SaveTraceWord();
            }
        }

        static SystemLogger()
        {
            LoadTraceWord();
        }

        private static void LoadTraceWord()
        {
            try
            {
                if (File.Exists(_traceWordPath))
                {
                    string content = File.ReadAllText(_traceWordPath).Trim();
                    int val;
                    if (int.TryParse(content, out val))
                    {
                        _activeFlags = (TraceFlags)val;
                        return;
                    }
                }
            }
            catch { }
            _activeFlags = TraceFlags.All;
        }

        private static void SaveTraceWord()
        {
            try
            {
                File.WriteAllText(_traceWordPath, ((int)_activeFlags).ToString());
            }
            catch { }
        }

        public static void Log(TraceFlags flag, string message)
        {
            if ((_activeFlags & flag) == 0) return;
            lock (_fileLock)
            {
                string line = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] [{1}] {2}",
                    DateTime.Now, flag, message);
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
        }

        public static void LogError(Exception ex, string context = "")
        {
            Log(TraceFlags.Error, string.Format("{0} - EXCEPTION: {1}", context, ex.Message));
        }
    }
}
