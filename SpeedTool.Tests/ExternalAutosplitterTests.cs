using System.Runtime.InteropServices;
using SpeedTool.Platform.Linux;

namespace SpeedTool.Tests;

public sealed class ExternalAutosplitterTests
{
    private const string MAPS = """
5596b2c00000-5596b2c01000 r--p 00000000 08:02 1234567  /home/user/games/My Game/Game.exe
5596b2c01000-5596b2c02000 rw-p 00001000 08:02 1234567  /home/user/games/My Game/Game.exe
7f1234000000-7f1234100000 r-xp 00000000 08:02 7654321  /usr/lib/x86_64-linux-gnu/liblua54.so
7f1235000000-7f1235100000 r-xp 00000000 08:02 7654322  /tmp/.mount_XXX/usr/bin/libglfw.so.3
""";

    [Fact]
    public void MapsParserFindsModuleBase()
    {
        Assert.Equal(0x5596b2c00000, ProcMaps.FindModuleBase(MAPS, "Game.exe"));
        Assert.Equal(0x7f1234000000, ProcMaps.FindModuleBase(MAPS, "liblua54.so"));
    }

    [Fact]
    public void MapsParserIsCaseInsensitive()
    {
        Assert.Equal(0x5596b2c00000, ProcMaps.FindModuleBase(MAPS, "game.exe"));
        Assert.Equal(0x7f1235000000, ProcMaps.FindModuleBase(MAPS, "LIBGLFW.SO.3"));
    }

    [Fact]
    public void MapsParserReturnsZeroForUnknownModule()
    {
        Assert.Equal(0, ProcMaps.FindModuleBase(MAPS, "other.exe"));
        Assert.Equal(0, ProcMaps.FindModuleBase("", "Game.exe"));
    }

    [Fact]
    public void MapsParserDoesNotMatchPartialNames()
    {
        var maps = "5596b2c00000-5596b2c01000 r--p 00000000 08:02 1  /games/MyGame.exe\n";
        Assert.Equal(0, ProcMaps.FindModuleBase(maps, "Game.exe"));
        Assert.Equal(0x5596b2c00000, ProcMaps.FindModuleBase(maps, "MyGame.exe"));
    }

    [Fact]
    public void MemoryReaderReadsOwnProcess()
    {
        var data = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        var pin = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            using var reader = new ProcessMemoryReader(Environment.ProcessId);
            var buffer = new byte[data.Length];
            Assert.True(reader.TryRead(pin.AddrOfPinnedObject().ToInt64(), buffer));
            Assert.Equal(data, buffer);
        }
        finally
        {
            pin.Free();
        }
    }

    [Fact]
    public void MemoryReaderFailsGracefullyOnBadAddress()
    {
        using var reader = new ProcessMemoryReader(Environment.ProcessId);
        var buffer = new byte[8];
        Assert.False(reader.TryRead(0x1, buffer));
    }

    [Fact]
    public void ProcessSearchReturnsMinusOneForMissingGame()
    {
        Assert.Equal(-1, LinuxProcesses.FindByExeName("definitely-not-a-running-game-xyz.exe"));
    }

    [Fact]
    public void ScriptEngineRunsScriptAndCallsBack()
    {
        var host = new FakeHost();
        host.Modules["game.exe"] = 0x400000;
        host.Memory[0x400000 + 0x1000] = 42;

        using var engine = new ExternalScriptEngine("""
            function on_load()
                base = module_base_address('game.exe')
                value = read_int(base + 0x1000)
            end

            function on_frame()
                if value == 42 then
                    timer_split()
                end
            end
        """, host);

        engine.OnFrame();

        Assert.Equal(1, host.SplitCalls);
    }

    [Fact]
    public void ScriptEngineSupportsPointerPathAndStrings()
    {
        var host = new FakeHost();
        host.Modules["game.exe"] = 0x400000;
        WritePointer(host, 0x400000 + 0x100, 0x500000);
        WritePointer(host, 0x500000 + 0x10, 0x600000);
        WriteAscii(host, 0x600000, "READY\0junk");

        using var engine = new ExternalScriptEngine("""
            function on_load()
                base = module_base_address('game.exe')
                ptr = pointer_path(base + 0x100, 0x10, 0)
                state = read_ascii(ptr, 10)
            end
        """, host);

        Assert.Equal(0x600000L, Convert.ToInt64(engine.GetGlobal("sandbox_env.ptr")));
        Assert.Equal("READY", engine.GetGlobal("sandbox_env.state"));
    }

    [Fact]
    public void ScriptEngineSandboxBlocksUnsafeFunctions()
    {
        var host = new FakeHost();
        using var engine = new ExternalScriptEngine("""
            function on_load()
                blocked = 0
            end

            function on_frame()
                blocked = 1
                os.execute("echo pwned")
                blocked = 2
            end
        """, host);

        engine.OnFrame();

        Assert.Equal(1L, Convert.ToInt64(engine.GetGlobal("sandbox_env.blocked")));
    }

    [Fact]
    public void GameTimeAccumulatorPausesWhileLoading()
    {
        var timer = new GameTimeAccumulator();
        timer.Start();
        timer.Cycle(false);
        Thread.Sleep(150);
        timer.Cycle(false);
        var before = timer.Value;
        Assert.True(before >= TimeSpan.FromMilliseconds(100));

        timer.Cycle(true);
        Thread.Sleep(150);
        timer.Cycle(true);
        var frozen = timer.Value;
        Thread.Sleep(100);
        timer.Cycle(true);

        Assert.True(timer.Value - frozen < TimeSpan.FromMilliseconds(60));
    }

    private static void WritePointer(FakeHost host, long address, long value)
    {
        var bytes = BitConverter.GetBytes(value);
        for(int i = 0; i < bytes.Length; i++)
            host.Memory[address + i] = bytes[i];
    }

    private static void WriteAscii(FakeHost host, long address, string value)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        for(int i = 0; i < bytes.Length; i++)
            host.Memory[address + i] = bytes[i];
    }

    private sealed class FakeHost : IScriptHost
    {
        public Dictionary<string, long> Modules = new();
        public Dictionary<long, byte> Memory = new();
        public int SplitCalls;

        public bool TryRead(long address, Span<byte> buffer)
        {
            for(int i = 0; i < buffer.Length; i++)
                buffer[i] = Memory.TryGetValue(address + i, out var value) ? value : (byte)0;
            return true;
        }

        public long GetModuleBase(string name)
        {
            return Modules.TryGetValue(name, out var value) ? value : 0;
        }

        public void Split()
        {
            SplitCalls++;
        }

        public void StartTimer()
        {
        }

        public void SetLoading(bool loading)
        {
        }
    }
}
