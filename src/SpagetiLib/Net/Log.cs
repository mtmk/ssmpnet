using System;

namespace SpagetiLib.Net
{
    internal static class Log
    {
        internal static void Debug(string tag, string message)
        {
//            Console.WriteLine("DEBUG [{0}] {1}", tag, message);
        }

        internal static void Info(string tag, string message)
        {
            Console.WriteLine("INFO [{0}] {1}", tag, message);
        }
    }
}