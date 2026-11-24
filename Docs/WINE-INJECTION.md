# Wine/Proton injection experiment

> **Status: experimental.** This branch makes the Windows injection stack
> reachable from Linux, but the EasyHook-under-Wine combination is
> fundamentally uncertain — treat this as a research branch, not a feature.
> The external autosplitter remains the default and is untouched unless you
> opt in (see below).
>
> **Experiment result (NFS MW 2012 / NFS13.exe, Proton Experimental, CachyOS,
> real .NET 4.8 installed with system wine):** the managed stages all work
> (real CLR loads under Proton, EasyHook loads, `RemoteHooking.InjectEx`
> runs), but the final native step fails with
> `STATUS_INTERNAL_ERROR: Unknown error in injected assembler code (Code: 5)`
> — EasyHook's injection assembler stub crashes inside the target because the
> code relies on `ntdll` threading/loader behavior Wine does not implement.
> Note the injector built on this branch is deliberately 32-bit: the 64-bit
> build takes EasyHook's cross-bitness WOW64-bypass path, which fails earlier
> (access denied reading the 32-bit target's module list). EasyHook-based
> injection into 32-bit Proton game processes is therefore a documented dead
> end. Injection *conceptually* still works under Proton via native methods
> (APC injection / manual PE mapping, e.g. the community `proton-injector`
> projects), but that requires a native bootstrap DLL instead of the managed
> EasyHook flow — significant additional work, not pursued in this branch.

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

Both reasons were confirmed on real hardware: reason 1 was removed by this
branch's TCP transport, and reason 2 kills the EasyHook flow at its native
injection stage (see the status note above).

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
only the transport changes. This is the **injection-only** branch: on Linux
TCP is the only transport and the memory-based external autosplitter has been
stripped, so any loadless timing you see is proven to come from injection.
Windows keeps the named pipe as before.

## How to test

1. **SpeedTool side** — use the AppImage built from this branch. No env var
   is needed: TCP on port 47774 is the default here.

   ```sh
   ./SpeedTool-injection-only-x86_64.AppImage
   ```

   (Optional: set `SPEEDTOOL_TCP_PORT` to use a different port.)

   The log will show `Injection-only branch: using injected timer` and
   `waiting for injected timer on TCP 127.0.0.1:47774`. Select your game,
   enter the script and set timing method to Loadless. Keep SpeedTool open.

2. **Windows-side tools** — the `wine-dist/` folder in this branch contains
   the compiled injector stack (`TimerInjector.exe`, `InjectedTimer.dll`,
   EasyHook runtime, NLua + `lua54.dll`). Copy it somewhere on disk, e.g.
   `~/SpeedToolInject`. Wine's `Z:` drive maps to `/` (the filesystem root), so
   the Windows path is `Z:\home\jon\SpeedToolInject\...` — but wine also accepts
   plain Unix paths, so `wine ~/SpeedToolInject/TimerInjector.exe` works.
   NOTE: `TimerInjector.exe` is built 32-bit on purpose. EasyHook requires the
   injector and the injected process to share one architecture; the 64-bit build
   takes a cross-bitness "WOW64 bypass" code path that Wine does not support
   (`WOW64Bypass.Install()` fails with access denied reading the 32-bit target's
   module list).

3. **Prefix requirements** (only needed once, in the game's prefix):

   ```sh
   # Real .NET Framework — EasyHook cannot host on wine-mono
   winetricks dotnet48
   ```

   With Proton, `protontricks <appid> dotnet48` is the normal path. Two known
   pitfalls:
   - Current Steam clients ship `appcache/appinfo` in a new sharded (V42)
     format that older protontricks versions cannot parse (upstream issues
     #516/#521). If protontricks crashes with `appinfo.vdf` FileNotFound or an
     `IndexError` in a vdf parser, bypass it entirely:
     `WINEPREFIX=<prefix> WINE="$(which wine)" winetricks -q dotnet48`
   - Proton's own wine cannot run the .NET 4.x installers at all — they abort
     with `returned status 67`. That is why the fallback above installs with
     the system's wine; the installed framework lives in the prefix and Proton
     reads it normally afterwards. A `runtime32.dll`/runtime error dialog on
     the first game launch after this is a known cosmetic side effect.

   Also make sure Proton's network isolation is off (it is by default),
   otherwise 127.0.0.1 traffic between SpeedTool and the game is blocked.

4. **Start the game** via Steam as usual, wait for it to reach a menu.

5. **Inject** — either by PID or by process name:

   ```sh
   # with only WINEPREFIX pointing at the game's prefix (no WINE override —
   # the injector itself must run under Proton's wine so it shares the
   # game's prefix):
   wine ~/SpeedToolInject/TimerInjector.exe NFS13.exe
   ```

   `TimerInjector.exe` accepts a numeric PID or an exe name.

6. **Lua runtime** — the injected DLL needs `lua54.dll` matching the game's
   bitness (32-bit for NFS13.exe). `wine-dist/lua54.dll` is the 32-bit copy,
   `wine-dist/lua54-x64.dll` the 64-bit one. If the game fails to load it,
   place the bitness-correct file in the game's own directory as `lua54.dll`.
   A wrong bitness here is the single most common failure: the timer connects
   and the script never runs.

7. Watch SpeedTool's debug log: `Pipe: injected timer connected over TCP`,
   then `InjectedTimer said: Game Auto-detected as DirectX 11` (or similar),
   then script `debug_message` output and timer ticks. If nothing connects:
   the SpeedTool side listens on 127.0.0.1:47774 from the moment the game
   profile is loaded, so load the profile first, then inject.

## If it fails

- **Injection error from EasyHook** (`There was an error while injecting`):
  most likely the CLR hosting or ntdll hooks — this is the known hard wall.
  Try `dotnet48` in the prefix; if EasyHook still fails, injection under Wine
  is a dead end and the external autosplitter remains the answer.
- **Connects but no frames**: the graphics autodetect hooks Wine's
  `wined3d`/`dxgi` implementations; a `debug_message Failed to auto-detect`
  means none of the supported APIs were found.
- **Rollback**: switch back to the `vibe/wayland-support-5f26ae` or
  `appimage-test` build (not this branch) and the proven memory-based
  external autosplitter is back. Nothing else changes.

## Accuracy note

Injection would *not* make the clock more accurate than the external
autosplitter: the injected Present hook fires once per game frame (16.7 ms at
60 fps), while the external poller ticks at 250–500 Hz. The clock source is
the same `Stopwatch` design on both platforms. What injection uniquely offers
is frame-synchronised script execution and no `ptrace_scope` requirement —
worth having if it works, but not an accuracy upgrade.
