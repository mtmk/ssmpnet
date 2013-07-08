using System;
using System.Linq;
using System.Reflection;

namespace Ssmpnet.Test
{
    public class Program
    {
        static void Main(string[] args)
        {
            var command = args[0];
            var action = args[1];

            Type commandType = Assembly.GetEntryAssembly().GetTypes()
                .First(t => t.Name.ToUpperInvariant() == command.ToUpperInvariant());
            
            MethodInfo actionMethod = commandType.GetMethods()
                .First(m => m.Name.ToLowerInvariant() == action.ToLowerInvariant());
            
            actionMethod.Invoke(Activator.CreateInstance(commandType), new object[] { args });
        }
    }
}
