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

    public static string[] GetRunningExeNames()
    {
        var names = new List<string>();
        foreach(var entry in Directory.GetDirectories("/proc"))
        {
            if(!int.TryParse(Path.GetFileName(entry), out var pid))
                continue;
            if(pid == Environment.ProcessId)
                continue;

            var name = GetProcessExeName(entry);
            if(name == "" || name == "SpeedTool")
                continue;
            names.Add(name);
        }

        return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToArray();
    }

    private static string GetProcessExeName(string procDir)
    {
        try
        {
            var args = File.ReadAllText($"{procDir}/cmdline").Split('\0', StringSplitOptions.RemoveEmptyEntries);
            var first = "";
            foreach(var arg in args)
            {
                var baseName = Path.GetFileName(arg.Replace('\\', '/'));
                if(baseName == "")
                    continue;
                if(first == "")
                    first = baseName;
                if(baseName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    return baseName;
            }

            if(first != "")
                return first;
            return File.ReadAllText($"{procDir}/comm").TrimEnd();
        }
        catch
        {
            return "";
        }
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
