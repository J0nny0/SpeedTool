using SpeedTool.Platform.Debugging;
using SpeedTool.Timer;

namespace SpeedTool.Platform.Linux;

public sealed class ExternalAutosplitter : IAutosplitter, IScriptHost
{
    public ExternalAutosplitter(string lookForExe)
    {
        exeName = lookForExe;
        InjectedExeName = lookForExe;
        DebugLog.SharedInstance.Write($"External autosplitter: watching for {lookForExe} (speedtool pid {Environment.ProcessId})");
        workerThread = new Thread(Worker);
        workerThread.IsBackground = true;
        workerThread.Start();
    }

    public string InjectedExeName { get; }

    public bool IsHooked { get; private set; }

    public TimeSpan CurrentTime => time;

    public TimerState CurrentState
    {
        get
        {
            if(time.Ticks == 0)
                return TimerState.NoState;
            if(Platform.SharedPlatform.IsRunFinished)
                return TimerState.Finished;
            return TimerState.Running;
        }
    }

    public void Cycle()
    {
    }

    public void ReloadScript(string script)
    {
        lock(sync)
        {
            pendingScript = script;
        }
    }

    public void Pause()
    {
    }

    public void Start()
    {
        lock(sync)
        {
            gameTimer = new GameTimeAccumulator();
            gameTimer.Start();
        }
    }

    public void Reset()
    {
        lock(sync)
        {
            gameTimer = null;
            scriptStarted = false;
            time = new(0);
        }
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
        closing = true;
        workerThread.Join();
        lock(sync)
        {
            engine?.Dispose();
            engine = null;
        }
        reader?.Dispose();
        reader = null;
    }

    private void Worker()
    {
        while(!closing)
        {
            if(!IsHooked)
            {
                Detach();
                Attach();
                continue;
            }

            if(!Directory.Exists($"/proc/{pid}"))
            {
                DebugLog.SharedInstance.Write("External autosplitter: game process is gone, re-attaching");
                Detach();
                continue;
            }

            try
            {
                lock(sync)
                {
                    if(pendingScript != null)
                    {
                        script = pendingScript;
                        pendingScript = null;
                        CreateEngine();
                    }

                    engine?.OnFrame();
                    if(gameTimer != null)
                    {
                        gameTimer.Cycle(isLoading);
                        if(scriptStarted)
                            time = gameTimer.Value;
                    }
                }
            }
            catch(Exception ex)
            {
                DebugLog.SharedInstance.Write("External autosplitter: script error: " + ex.Message);
            }

            if((DateTime.UtcNow - lastHeartbeat).TotalSeconds >= HEARTBEAT_SECONDS)
            {
                lastHeartbeat = DateTime.UtcNow;
                var failures = reader?.FailedReads ?? 0;
                var lastFail = reader?.LastFailedAddress ?? 0;
                DebugLog.SharedInstance.Write($"External autosplitter: heartbeat speedtoolPid={Environment.ProcessId} pid={pid} loading={isLoading} " +
                                              $"scriptStarted={scriptStarted} time={time} readFailures={failures} lastFail=0x{lastFail:X}");
            }

            Thread.Sleep(FRAME_PACING_MS);
        }
    }

    private void Attach()
    {
        var found = LinuxProcesses.FindByExeName(exeName);
        if(found == -1)
        {
            Thread.Sleep(500);
            return;
        }

        try
        {
            reader = new ProcessMemoryReader(found);
            pid = found;
            LogAttachInfo(found);
            lock(sync)
            {
                if(pendingScript != null)
                {
                    script = pendingScript;
                    pendingScript = null;
                }
                CreateEngine();
            }
            IsHooked = true;
            DebugLog.SharedInstance.Write($"External autosplitter: attached to {exeName} (pid {found}, speedtool pid {Environment.ProcessId})");
        }
        catch(Exception ex)
        {
            DebugLog.SharedInstance.Write($"External autosplitter: failed to attach to {exeName}: {ex.Message}");
            if(ex is UnauthorizedAccessException or System.IO.IOException)
                DebugLog.SharedInstance.Write("External autosplitter: reading another process' memory may require `sysctl kernel.yama.ptrace_scope=0`");
            Detach();
            Thread.Sleep(1000);
        }
    }

    private void Detach()
    {
        lock(sync)
        {
            engine?.Dispose();
            engine = null;
        }
        reader?.Dispose();
        reader = null;
        pid = -1;
        pointerSize = 8;
        pointerSizeDetected = false;
        IsHooked = false;
    }

    private void LogAttachInfo(int found)
    {
        try
        {
            var baseAddress = ProcMaps.GetModuleBase(found, exeName);
            Span<byte> magic = stackalloc byte[2];
            var magicRead = reader!.TryRead(baseAddress, magic);
            var magicText = magicRead ? $"{(char)magic[0]}{(char)magic[1]}" : "unreadable";
            DebugLog.SharedInstance.Write($"External autosplitter: {exeName} base=0x{baseAddress:X} magic={magicText}");
        }
        catch(Exception ex)
        {
            DebugLog.SharedInstance.Write($"External autosplitter: attach info failed: {ex.Message}");
        }
    }

    private void CreateEngine()
    {
        engine?.Dispose();
        engine = new ExternalScriptEngine(script ?? "", this);
    }

    bool IScriptHost.TryRead(long address, Span<byte> buffer)
    {
        return reader != null && reader.TryRead(address, buffer);
    }

    long IScriptHost.GetModuleBase(string name)
    {
        if(pid == -1)
            return 0;

        try
        {
            var baseAddress = ProcMaps.GetModuleBase(pid, name);
            if(baseAddress != 0 && !pointerSizeDetected)
            {
                pointerSizeDetected = true;
                DetectPointerSize(baseAddress);
            }
            return baseAddress;
        }
        catch
        {
            return 0;
        }
    }

    int IScriptHost.PointerSize => pointerSize;

    private void DetectPointerSize(long baseAddress)
    {
        pointerSize = 8;
        Span<byte> lfanew = stackalloc byte[4];
        if(!reader!.TryRead(baseAddress + 0x3C, lfanew))
            return;
        var peOffset = BitConverter.ToInt32(lfanew);
        Span<byte> machine = stackalloc byte[2];
        if(!reader.TryRead(baseAddress + peOffset + 4, machine))
            return;
        var machineType = BitConverter.ToUInt16(machine);
        pointerSize = machineType is 0x014C or 0x01C4 ? 4 : 8;
        DebugLog.SharedInstance.Write($"External autosplitter: {exeName} machine=0x{machineType:X4} ({(pointerSize == 4 ? "32" : "64")}-bit pointers)");
    }

    void IScriptHost.Split()
    {
        Platform.SharedPlatform.Split();
    }

    void IScriptHost.StartTimer()
    {
        DebugLog.SharedInstance.Write($"External autosplitter: script requested timer start (run already started: {Platform.SharedPlatform.IsRunStarted})");
        if(!Platform.SharedPlatform.IsRunStarted)
            Platform.SharedPlatform.Split();
        scriptStarted = true;
    }

    void IScriptHost.SetLoading(bool loading)
    {
        isLoading = loading;
    }

    private const int FRAME_PACING_MS = 4;
    private const int HEARTBEAT_SECONDS = 10;

    private readonly object sync = new();
    private readonly string exeName;
    private readonly Thread workerThread;

    private ProcessMemoryReader? reader;
    private ExternalScriptEngine? engine;
    private GameTimeAccumulator? gameTimer;
    private string? script;
    private string? pendingScript;
    private TimeSpan time;
    private int pid = -1;
    private int pointerSize = 8;
    private bool pointerSizeDetected;
    private bool isLoading;
    private bool scriptStarted;
    private bool closing;
    private DateTime lastHeartbeat = DateTime.MinValue;
}
