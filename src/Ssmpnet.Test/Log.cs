using System;
using System.Diagnostics;
using System.Threading;

namespace Ssmpnet.Test
{
    internal static class Log
    {
        internal static void Error(string tag, string format, params object[] args)
        {
            //ConsoleWriteLine("ERROR", tag, format, args);
        }

        [Conditional("TRACE")]
        internal static void Info(string tag, string format, params object[] args)
        {
            //ConsoleWriteLine("INFO", tag, format, args);
        }

        [Conditional("DEBUG")]
        internal static void Debug(string tag, string format, params object[] args)
        {
            //ConsoleWriteLine("DEBUG", tag, format, args);
        }

        internal static void ConsoleWriteLine(string level, string tag, string format, params object[] args)
        {
            Console.WriteLine("[{0}] Thread[ID:{3} TP:{4}] {1} - {2}",
                level,
                tag,
                string.Format(format, args)
                ,Thread.CurrentThread.ManagedThreadId
                ,Thread.CurrentThread.IsThreadPoolThread
                );
        }
    }
}