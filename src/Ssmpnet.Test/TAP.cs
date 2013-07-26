using System;

namespace TAP
{
    public static class Plan
    {
        public static void Tests(int numberOfTest)
        {
            Console.Out.WriteLine("1.." + numberOfTest);
        }
    }

    public static class Assert
    {
        public static void Ok(bool value, string message = null)
        {
            if (value) Ok(message);
            else NotOk(message);
        }

        public static void Ok(string message = null)
        {
            Console.Out.WriteLine("ok" + (message != null ? " - " + message : ""));
        }
        
        public static void NotOk(string message = null)
        {
            Console.Out.WriteLine("not ok" + (message != null ? " - " + message : ""));
        }
    }
}