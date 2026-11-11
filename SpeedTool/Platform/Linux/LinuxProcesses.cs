namespace SpeedTool.Platform.Linux;

internal static class LinuxProcesses
{
    public static int FindByExeName(string exeName)
    {
        foreach(var entry in Directory.GetDirectories("/proc"))
        {
            if(!int.TryParse(Path.GetFileName(entry), out var pid))
                continue;
            if(pid == Environment.ProcessId)
                continue;

            if(Matches(entry, exeName))
                return pid;
        }

        return -1;
    }

    private static bool Matches(string procDir, string exeName)
    {
        try
        {
            foreach(var arg in File.ReadAllText($"{procDir}/cmdline").Split('\0', StringSplitOptions.RemoveEmptyEntries))
            {
                if(arg.Equals(exeName, StringComparison.OrdinalIgnoreCase) ||
                   arg.EndsWith("/" + exeName, StringComparison.OrdinalIgnoreCase) ||
                   arg.EndsWith("\\" + exeName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            var comm = File.ReadAllText($"{procDir}/comm").TrimEnd();
            return comm.Equals(exeName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
