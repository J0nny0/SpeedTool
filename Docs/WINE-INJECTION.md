# Wine/Proton injection experiment

> **Status: experimental.** This branch makes the Windows injection stack
> reachable from Linux, but the EasyHook-under-Wine combination is
> fundamentally uncertain — treat this as a research branch, not a feature.
> The external autosplitter remains the default and is untouched unless you
> opt in (see below).

## Why injection is attractive (and why it may not work)

On Windows, SpeedTool's standout feature is **injected timing**: a DLL inside
the game hooks the graphics API, runs the Lua script on the game's own frames
and reports ticks over a named pipe. Under Proton/Wine this breaks for two
reasons:

1. A Linux process cannot open a Wine named pipe (`\\.\pipe\SpeedToolPipe`),
   so the injected DLL and SpeedTool could never talk.
2. EasyHook relies on undocumented `ntdll` internals that Wine only partially
   implements, and injecting the .NET CLR into a Wine process requires a real
   .NET Framework in the prefix (wine-mono is usually not enough).

This branch removes reason 1 entirely and gives you the tools to test
reason 2 on real hardware.

## Architecture on this branch

```
SpeedTool (Linux, AppImage)                Wine prefix
┌───────────────────────────┐              ┌──────────────────────────────┐
│ TCP server 127.0.0.1:47774│◄── TCP ─────►│ InjectedTimer.dll (in game)  │
│ same line protocol as the │              │  ├─ EasyHook Present hook    │
│ named pipe                │              │  ├─ Lua engine (NLua)        │
└───────────────────────────┘              │  └─ timer ticks / start/split│
        ▲                                  └──────────────▲───────────────┘
        │ script, start, reset                            │ RemoteHooking.Inject
        └─────────────────────────── ┌────────────────────┴───────────────┐
                                     │ TimerInjector.exe (run under wine) │
                                     └────────────────────────────────────┘
```

The message protocol is byte-for-byte the Windows one (`script <line>`,
`script_load`, `start`, `reset`, `timer <ticks>`, `split`, `debug_message`),
only the transport changes. Both sides fall back to the original named pipe
when `SPEEDTOOL_TCP_PORT` is not set, so Windows behaviour is unchanged.

## How to test

1. **SpeedTool side** — use a build from this branch and enable TCP mode:

   ```sh
   SPEEDTOOL_TCP_PORT=47774 ./SpeedTool-wayland-x86_64.AppImage
   ```

   The log will show `Injection experiment enabled` and
   `waiting for injected timer on TCP 127.0.0.1:47774`. Select your game,
   enter the script and set timing method to Loadless. Keep SpeedTool open.

2. **Windows-side tools** — the `wine-dist/` folder in this branch contains
   the compiled injector stack (`TimerInjector.exe`, `InjectedTimer.dll`,
   EasyHook runtime, NLua + `lua54.dll`). Copy it somewhere on disk, e.g.
   `~/SpeedToolInject`. Wine's `Z:` drive maps to `/`, so it is reachable
   from the prefix as `Z:\SpeedToolInject`.

3. **Prefix requirements** (only needed once, in the game's prefix):

   ```sh
   # Real .NET Framework — EasyHook cannot host on wine-mono
   winetricks dotnet48
   ```

   With Proton, use `protontricks <appid> dotnet48`. Also make sure Proton's
   network isolation is off (it is by default), otherwise 127.0.0.1 traffic
   between SpeedTool and the game is blocked.

4. **Start the game** via Steam as usual, wait for it to reach a menu.

5. **Inject** — either by PID or by process name:

   ```sh
   # find the game's *Windows* PID inside the prefix:
   protontricks -c 'wine taskmgr' <appid>
   # then, from the prefix (example via protontricks):
   protontricks -c 'wine Z:\\SpeedToolInject\\TimerInjector.exe NFS13.exe' <appid>
   ```

   `TimerInjector.exe` accepts a numeric PID or an exe name.

6. **Lua runtime** — the injected DLL needs `lua54.dll` matching the game's
   bitness (32-bit for NFS13.exe). A copy ships in `wine-dist/`; if the game
   fails to load it, place it in the game's own directory.

7. Watch SpeedTool's debug log: `Pipe: injected timer connected over TCP`,
   then `InjectedTimer said: Game Auto-detected as DirectX 11` (or similar),
   then script `debug_message` output and timer ticks.

## If it fails

- **Injection error from EasyHook** (`There was an error while injecting`):
  most likely the CLR hosting or ntdll hooks — this is the known hard wall.
  Try `dotnet48` in the prefix; if EasyHook still fails, injection under Wine
  is a dead end and the external autosplitter remains the answer.
- **Connects but no frames**: the graphics autodetect hooks Wine's
  `wined3d`/`dxgi` implementations; a `debug_message Failed to auto-detect`
  means none of the supported APIs were found.
- **Rollback**: unset `SPEEDTOOL_TCP_PORT` (or use a build from `main`) and
  the external autosplitter takes over again. Nothing else changes.

## Accuracy note

Injection would *not* make the clock more accurate than the external
autosplitter: the injected Present hook fires once per game frame (16.7 ms at
60 fps), while the external poller ticks at 250–500 Hz. The clock source is
the same `Stopwatch` design on both platforms. What injection uniquely offers
is frame-synchronised script execution and no `ptrace_scope` requirement —
worth having if it works, but not an accuracy upgrade.
