using System;
using System.Diagnostics;

namespace Ssmpnet
{
    internal static class Log
    {
        private static readonly TraceSource TraceSource = new TraceSource("Ssmpnet");

        internal static void Error(string tag, string format, params object[] args)
        {
            WriteLog(TraceEventType.Error, 1, tag, format, args);
        }

        [Conditional("TRACE")]
        internal static void Info(string tag, string format, params object[] args)
        {
            WriteLog(TraceEventType.Information, 2, tag, format, args);
        }

        [Conditional("DEBUG")]
        internal static void Debug(string tag, string format, params object[] args)
        {
            WriteLog(TraceEventType.Verbose, 3, tag, format, args);
        }

        private static void WriteLog(TraceEventType level, int id, string tag, string format, params object[] args)
        {
            TraceSource.TraceEvent(level, id, "[" + tag + "] " + format, args);
            Console.WriteLine(string.Format("[" + tag + "] " + format, args));
        }
    }
}