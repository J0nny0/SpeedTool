using System.Diagnostics;
using System.Runtime.InteropServices;
using SpeedTool.Platform.Linux;

namespace SpeedTool.Platform;

internal static class ProcessList
{
    internal static string[] GetRunningExeNames()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Process.GetProcesses()
                .Select(p => p.ProcessName + ".exe")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n)
                .ToArray();
        }

        return LinuxProcesses.GetRunningExeNames();
    }
}
