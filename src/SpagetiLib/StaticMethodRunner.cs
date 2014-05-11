using System;
using System.Reflection;

namespace SpagetiLib
{
    public static class StaticMethodRunner
    {
        public static void Run(this Type type, string[] args)
        {
            var methodInfo = type.GetMethod(args[0], BindingFlags.Static | BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic);

            if (methodInfo != null)
                methodInfo.Invoke(null, new object[] { args });
            else
                Console.WriteLine("Cannot find command!");

        }
    }
}