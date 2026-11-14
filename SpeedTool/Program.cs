using SpeedTool.Global;
using SpeedTool.Platform;
using SpeedTool.Platform.Debugging;
using SpeedTool.Util;
using SpeedTool.Windows;

var configPath = GetConfigPath();
Console.WriteLine(configPath);

// Last-resort crash capture: anything that escapes the window loop or fires on a
// background thread must leave a stack trace in the debug log before we die
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    DebugLog.SharedInstance.Write($"Unhandled exception, terminating={args.IsTerminating}");
    if(args.ExceptionObject is Exception ex)
        DebugLog.SharedInstance.Write(ex.ToString());
};

Configuration.Init(configPath);
Platform.SharedPlatform.AddWindow(new MainWindow());

Platform.SharedPlatform.Run();

Platform.SharedPlatform.Release();

static string GetConfigPath()
{
    // Prefer the config next to the executable, but fall back to a writable location,
    // e.g. when running from an AppImage or any other read-only location
    var local = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if(File.Exists(local) || IsDirectoryWritable(Path.GetDirectoryName(local)!))
        return local;

    Directory.CreateDirectory(ENV.LocalFilesPath);
    return Path.Combine(ENV.LocalFilesPath, "appsettings.json");
}

static bool IsDirectoryWritable(string dir)
{
    try
    {
        var probe = Path.Combine(dir, Path.GetRandomFileName());
        using(File.Create(probe))
        {
        }
        File.Delete(probe);
        return true;
    }
    catch
    {
        return false;
    }
}
