using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using SpeedTool.Global;
using SpeedTool.Global.Definitions;
using SpeedTool.Platform.Debugging;
using SpeedTool.Platform.Linux;
using SpeedTool.Splits;
using SpeedTool.Util;
using SpeedTool.Windows;

namespace SpeedTool.Platform;

internal sealed class GameWatcher
{
    public GameWatcher()
    {
        settings = Configuration.GetSection<GameWatcherSettings>();
        Configuration.OnConfigurationChanged += OnConfigChanged;
        workerThread = new Thread(Worker) { IsBackground = true };
        workerThread.Start();
    }

    public void Register(string path)
    {
        if(path == "")
            return;
        lock(sync)
        {
            if(!settings.KnownGames.Contains(path))
            {
                settings.KnownGames.Add(path);
                Configuration.SetSection(settings);
            }
            exeNames.Remove(path);
        }
        DebugLog.SharedInstance.Write($"GameWatcher: registered {path}");
    }

    internal static bool ShouldLoad(int candidatePid, string candidateExe, string? lastLoadedExe, int lastLoadedPid)
    {
        if(candidatePid == -1)
            return false;
        if(lastLoadedExe != null && candidateExe.Equals(lastLoadedExe, StringComparison.OrdinalIgnoreCase) &&
           candidatePid == lastLoadedPid)
            return false;
        return true;
    }

    public string? FindGameFileForExe(string exeName)
    {
        lock(sync)
        {
            foreach(var path in settings.KnownGames)
            {
                if(GetExeName(path).Equals(exeName, StringComparison.OrdinalIgnoreCase))
                    return path;
            }
        }
        return null;
    }

    public void Ignore(string exeName)
    {
        lock(sync)
        {
            ignored.Add(exeName);
        }
        DebugLog.SharedInstance.Write($"GameWatcher: ignoring {exeName}");
    }

    private void OnConfigChanged(object? sender, IConfigurationSection section)
    {
        if(section is GameWatcherSettings s)
            settings = s;
    }

    private void Worker()
    {
        while(true)
        {
            Thread.Sleep(POLL_PERIOD_MS);
            try
            {
                Cycle();
            }
            catch
            {
            }
        }
    }

    private void Cycle()
    {
        var p = Platform.SharedPlatform;
        if(p.Game != null)
        {
            lastLoadedExe = p.Game.ExeName;
            lastLoadedPid = FindProcessPid(p.Game.ExeName);
            return;
        }

        if(!settings.AutoLoadGame)
            return;

        lock(sync)
        {
            foreach(var path in settings.KnownGames)
            {
                var exeName = GetExeName(path);
                if(exeName == "")
                    continue;

                var pid = FindProcessPid(exeName);
                if(!ShouldLoad(pid, exeName, lastLoadedExe, lastLoadedPid))
                    continue;

                DebugLog.SharedInstance.Write($"GameWatcher: {exeName} is running, auto-loading game from {path}");
                try
                {
                    p.LoadGame(Game.LoadFromFile(path));
                }
                catch
                {
                    DebugLog.SharedInstance.Write($"GameWatcher: failed to load {path}");
                }
                return;
            }
        }

        PromptForUnknownGames();
    }

    private void PromptForUnknownGames()
    {
        HashSet<string> known;
        lock(sync)
        {
            known = settings.KnownGames
                .Select(path => GetExeName(path))
                .Where(n => n != "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        foreach(var exe in ProcessList.GetRunningExeNames())
        {
            if(!exe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                continue;
            lock(sync)
            {
                if(known.Contains(exe) || ignored.Contains(exe) || prompted.Contains(exe))
                    continue;
                prompted.Add(exe);
            }
            DebugLog.SharedInstance.Write($"GameWatcher: unknown game {exe} is running");
            Platform.SharedPlatform.AddWindow(new ProcessPickerWindow(exe));
            return;
        }
    }

    private string GetExeName(string path)
    {
        lock(sync)
        {
            if(exeNames.TryGetValue(path, out var cached))
                return cached;
        }

        var exeName = "";
        try
        {
            using var zip = ZipFile.OpenRead(path);
            var entry = zip.GetEntry("meta.json");
            if(entry != null)
            {
                var obj = JSONHelper.EnforceParseAsObject(entry.AsText());
                if(obj.ContainsKey("ExeName"))
                    exeName = obj.EnforceGetString("ExeName");
            }
        }
        catch
        {
            exeName = "";
        }

        lock(sync)
        {
            exeNames[path] = exeName;
        }
        return exeName;
    }

    private static int FindProcessPid(string exeName)
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var procs = Process.GetProcessesByName(exeName.Replace(".exe", ""));
            return procs.Length > 0 ? procs[0].Id : -1;
        }
        return LinuxProcesses.FindByExeName(exeName);
    }

    private readonly object sync = new();
    private readonly Thread workerThread;
    private readonly Dictionary<string, string> exeNames = new();

    private GameWatcherSettings settings;
    private string? lastLoadedExe;
    private int lastLoadedPid = -1;
    private readonly HashSet<string> ignored = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> prompted = new(StringComparer.OrdinalIgnoreCase);

    private const int POLL_PERIOD_MS = 1000;
}
