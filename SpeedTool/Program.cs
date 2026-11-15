using System.Runtime.InteropServices;
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

NativeCrashCapture.Install();

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

/// <summary>
/// Catches fatal POSIX signals (abort, illegal instruction, bus error) that bypass all
/// managed exception handlers, so a native crash still leaves a trace in the debug log.
/// The handler only does a raw write(2) to a pre-opened fd: anything else is not
/// async-signal-safe. SIGSEGV is deliberately NOT touched: the runtime owns it.
/// </summary>
static class NativeCrashCapture
{
    public static void Install()
    {
        if(!OperatingSystem.IsLinux())
            return;

        try
        {
            Directory.CreateDirectory(ENV.LocalFilesPath);
            logFd = open(Path.Combine(ENV.LocalFilesPath, "debug.log"), O_WRONLY | O_APPEND | O_CREAT, 0b110100100);
            if(logFd < 0)
                return;

            InstallOne(SIGABRT);
            InstallOne(SIGILL);
            InstallOne(SIGBUS);
            DebugLog.SharedInstance.Write("NativeCrashCapture: SIGABRT/SIGILL/SIGBUS handlers installed");
        }
        catch(Exception ex)
        {
            // Signal capture is diagnostic only; never block startup over it
            DebugLog.SharedInstance.Write($"NativeCrashCapture: installation failed: {ex.Message}");
        }
    }

    static unsafe void InstallOne(int signum)
    {
        var text = System.Text.Encoding.UTF8.GetBytes($"NATIVE CRASH: signal {signum}\n");
        var ptr = Marshal.AllocHGlobal(text.Length);
        Marshal.Copy(text, 0, ptr, text.Length);
        messages[signum] = (ptr, (nuint)text.Length);

        signal(signum, (nint)(delegate* unmanaged<int, void>)&OnSignal);
    }

    [UnmanagedCallersOnly]
    static unsafe void OnSignal(int signum)
    {
        if(messages.TryGetValue(signum, out var msg))
            write(logFd, (void*)msg.Ptr, msg.Len);
    }

    static readonly Dictionary<int, (IntPtr Ptr, nuint Len)> messages = new();
    static int logFd = -1;

    const int O_WRONLY = 1;
    const int O_CREAT = 0x40;
    const int O_APPEND = 0x400;
    const int SIGILL = 4;
    const int SIGABRT = 6;
    const int SIGBUS = 7;

    [DllImport("libc", SetLastError = true)]
    static extern int open(string path, int flags, uint mode);

    [DllImport("libc", SetLastError = true)]
    static extern int signal(int signum, nint handler);

    [DllImport("libc", SetLastError = true)]
    static extern unsafe long write(int fd, void* buf, nuint count);
}
