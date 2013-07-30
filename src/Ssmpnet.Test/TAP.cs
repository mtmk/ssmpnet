using System;

namespace TAP
{
    public static class Plan
    {
        public static void Tests(int numberOfTest)
        {
            Console.Out.WriteLine("1.." + numberOfTest);
        }

        public static void BenchName(string name)
        {
            Console.Out.WriteLine("# BENCH NAME: " + name);
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
        
        public static void Comment(string format, params object[] args)
        {
            Console.Out.WriteLine("#" + (format != null ? " " + string.Format(format, args) : ""));
        }
        
        public static void BenchVar(string name, object value, string unit)
        {
            Console.Out.WriteLine("# BENCH VAR: {0} = {1} {2}", name, value, unit);
        }
    }
}