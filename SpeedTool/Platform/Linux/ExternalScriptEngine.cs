using System.Reflection;
using System.Text;
using System.Linq;
using NLua;
using SpeedTool.Platform.Debugging;

namespace SpeedTool.Platform.Linux;

internal interface IScriptHost
{
    bool TryRead(long address, Span<byte> buffer);

    long GetModuleBase(string name);

    void Split();

    void StartTimer();

    void SetLoading(bool loading);
}

internal sealed class ExternalScriptEngine : IDisposable
{
    public ExternalScriptEngine(string script, IScriptHost host)
    {
        this.host = host;
        luaState = new Lua(true);
        luaState.DoString(SANDBOX);

        Register("module_base_address", (Func<string, long>)(name => host.GetModuleBase(name)));
        Register("__report_error", (Action<string>)ReportError);
        Register("debug_message", (Action<string>)(message => DebugLog.SharedInstance.Write("Script: " + message)));
        Register("debug_message_address", (Action<long>)(address => DebugLog.SharedInstance.Write($"Script: 0x{address:X16}")));
        Register("timer_set_loading", (Action)(() => host.SetLoading(true)));
        Register("timer_set_not_loading", (Action)(() => host.SetLoading(false)));
        Register("timer_split", (Action)(() => host.Split()));
        Register("timer_start", (Action)(() => host.StartTimer()));
        Register("read_int", (Func<long, int>)ReadInt32);
        Register("read_long", (Func<long, long>)ReadInt64);
        Register("read_float", (Func<long, float>)ReadFloat);
        Register("read_double", (Func<long, double>)ReadDouble);
        Register("read_ascii", (Func<long, int, string>)ReadASCII);
        Register("read_bytes", (Func<long, int, byte[]>)ReadBytes);
        Register("pointer_path_impl", (Func<LuaTable, long>)PointerPath);
        luaState.DoString("function pointer_path(...) return pointer_path_impl({...}) end");
        luaState.DoString("sandbox_env.pointer_path = pointer_path");

        luaState["__script"] = script;
        luaState.DoString(LOADER);

        DebugLog.SharedInstance.Write($"External autosplitter: script loaded ({script.Length} chars, " +
                                      $"on_load={luaState["sandbox_env.on_load"] != null}, on_frame={luaState["sandbox_env.on_frame"] != null})");
    }

    public void OnFrame()
    {
        luaState.DoString("run_sandbox(sandbox_env, sandbox_env.on_frame)");
    }

    public object? GetGlobal(string name)
    {
        return luaState[name];
    }

    public void Dispose()
    {
        luaState.Dispose();
    }

    private void ReportError(string message)
    {
        if(message == lastError)
            return;
        lastError = message;
        DebugLog.SharedInstance.Write("External autosplitter: script error: " + message);
    }

    private void Register(string name, Delegate function)
    {
        luaState.RegisterFunction(name, function.Target, function.GetMethodInfo());
        luaState.DoString($"sandbox_env.{name} = {name}");
    }

    private int ReadInt32(long address)
    {
        Span<byte> buffer = stackalloc byte[4];
        if(!host.TryRead(address, buffer))
            return 0;
        return BitConverter.ToInt32(buffer);
    }

    private long ReadInt64(long address)
    {
        Span<byte> buffer = stackalloc byte[8];
        if(!host.TryRead(address, buffer))
            return 0;
        return BitConverter.ToInt64(buffer);
    }

    private float ReadFloat(long address)
    {
        Span<byte> buffer = stackalloc byte[4];
        if(!host.TryRead(address, buffer))
            return 0;
        return BitConverter.ToSingle(buffer);
    }

    private double ReadDouble(long address)
    {
        Span<byte> buffer = stackalloc byte[8];
        if(!host.TryRead(address, buffer))
            return 0;
        return BitConverter.ToDouble(buffer);
    }

    private byte[] ReadBytes(long address, int amount)
    {
        var data = new byte[amount];
        host.TryRead(address, data);
        return data;
    }

    private string ReadASCII(long address, int count)
    {
        var data = ReadBytes(address, count);
        int length = 0;
        while(length < data.Length && data[length] != 0)
            length++;
        return Encoding.ASCII.GetString(data, 0, length);
    }

    private long PointerPath(LuaTable addresses)
    {
        var path = addresses.Keys.Cast<object>()
            .Select(key => (index: Convert.ToInt64(key), value: Convert.ToInt64(addresses[key])))
            .OrderBy(entry => entry.index)
            .Select(entry => entry.value)
            .ToArray();

        long begin = ReadInt64(path[0]);
        for(int i = 1; i < path.Length - 1; i++)
            begin = ReadInt64(begin + path[i]);

        return begin + path[^1];
    }

    private readonly IScriptHost host;

    private readonly Lua luaState;

    // The script chunk is loaded with `sandbox_env` as its _ENV, so global lookups inside
    // on_load/on_frame resolve against the whitelist. run_sandbox alone would not do that.
    private const string LOADER =
"""
local e=_ENV
local chunk, err = e.load(__script, 'script', 't', sandbox_env)
if not chunk then error(err) end
local ok, rerr = e.pcall(chunk)
if not ok then error(rerr) end
__script = nil
run_sandbox(sandbox_env, sandbox_env.on_load)
""";

    // Original LUA sandbox code is taken from here https://stackoverflow.com/questions/1224708/how-can-i-create-a-secure-lua-sandbox
    private const string SANDBOX =
"""
-- save a pointer to globals that would be unreachable in sandbox
local e=_ENV

-- sample sandbox environment
sandbox_env = {
  ipairs = ipairs,
  next = next,
  pairs = pairs,
  pcall = pcall,
  tonumber = tonumber,
  tostring = tostring,
  type = type,
  unpack = unpack,
  coroutine = { create = coroutine.create, resume = coroutine.resume,
      running = coroutine.running, status = coroutine.status,
      wrap = coroutine.wrap },
  string = { byte = string.byte, char = string.char, find = string.find,
      format = string.format, gmatch = string.gmatch, gsub = string.gsub,
      len = string.len, lower = string.lower, match = string.match,
      rep = string.rep, reverse = string.reverse, sub = string.sub,
      upper = string.upper },
  table = { insert = table.insert, maxn = table.maxn, remove = table.remove,
      sort = table.sort },
  math = { abs = math.abs, acos = math.acos, asin = math.asin,
      atan = math.atan, atan2 = math.atan2, ceil = math.ceil, cos = math.cos,
      cosh = math.cosh, deg = math.deg, exp = math.exp, floor = math.floor,
      fmod = math.fmod, frexp = math.frexp, huge = math.huge,
      ldexp = math.ldexp, log = math.log, log10 = math.log10, max = math.max,
      min = math.min, modf = math.modf, pi = math.pi, pow = math.pow,
      rad = math.rad, random = math.random, sin = math.sin, sinh = math.sinh,
      sqrt = math.sqrt, tan = math.tan, tanh = math.tanh },
  os = { clock = os.clock, difftime = os.difftime, time = os.time },
}

function run_sandbox(sb_env, sb_func, ...)
  local sb_orig_env=_ENV
  if (not sb_func) then return nil end
  _ENV=sb_env
  local sb_ret={e.pcall(sb_func, ...)}
  _ENV=sb_orig_env
  if (not sb_ret[1]) then e.__report_error(e.tostring(sb_ret[2])) end
  return e.table.unpack(sb_ret)
end
""";

    private string? lastError;
}
