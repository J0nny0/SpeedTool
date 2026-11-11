namespace SpeedTool.Platform.Linux;

internal static class ProcMaps
{
    public static long GetModuleBase(int pid, string moduleName)
    {
        return FindModuleBase(File.ReadAllText($"/proc/{pid}/maps"), moduleName);
    }

    public static long FindModuleBase(string mapsContent, string moduleName)
    {
        foreach(var line in mapsContent.Split('\n'))
        {
            var path = ExtractPath(line);
            if(path == null)
                continue;

            if(path.EndsWith("/" + moduleName, StringComparison.OrdinalIgnoreCase))
                return ParseStartAddress(line);
        }

        return 0;
    }

    private static string? ExtractPath(string line)
    {
        var idx = line.IndexOf(" /", StringComparison.Ordinal);
        if(idx == -1)
            return null;

        return line.Substring(idx + 1).TrimEnd();
    }

    private static long ParseStartAddress(string line)
    {
        var hex = line.AsSpan(0, line.IndexOf('-'));
        return long.Parse(hex, System.Globalization.NumberStyles.HexNumber);
    }
}
