using System;
using System.IO;

using EasyHook;

class Program
{
    static int Main(string[] args)
    {
        if(args.Length != 1)
        {
            Console.WriteLine("Usage: TimerInjector.exe <pid | process name>");
            return 1;
        }
        int targetPID = 0;
        if(!int.TryParse(args[0], out targetPID))
        {
            var name = args[0];
            if(name.ToLower().EndsWith(".exe"))
                name = name.Substring(0, name.Length - 4);
            var procs = System.Diagnostics.Process.GetProcessesByName(name);
            if(procs.Length == 0)
            {
                Console.WriteLine("Process not found: " + args[0]);
                return 2;
            }
            targetPID = procs[0].Id;
        }

        if (targetPID <= 0)
            return 1;

        // Get the full path to the assembly we want to inject into the target process
        string injectionLibrary = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "InjectedTimer.dll");

        try
        {
            // TCP transport is enabled by setting SPEEDTOOL_TCP_PORT for the injector;
            // an empty value keeps the default named pipe transport.
            var tcpPort = Environment.GetEnvironmentVariable("SPEEDTOOL_TCP_PORT") ?? "";

            // inject into existing process
            RemoteHooking.Inject(
                targetPID,          // ID of process to inject into
                injectionLibrary,   // 32-bit library to inject (if target is 32-bit)
                injectionLibrary,   // 64-bit library to inject (if target is 64-bit)
                    "",             // the parameters to pass into injected library
                tcpPort
            );
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("There was an error while injecting into target:");
            Console.ResetColor();
            Console.WriteLine(e.ToString());
        }

        return 0;
    }
}
