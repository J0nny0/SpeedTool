# SpeedTool — Wayland & Linux changes

This fork of [3RD-Maple/SpeedTool](https://github.com/3RD-Maple/SpeedTool) contains
Linux/Wayland fixes and a Linux autosplitter that are **not** part of upstream.
This document describes every change, why it was made, and how to use the result.

Branch layout of this fork:

| Branch | Contents |
| :--- | :--- |
| `main` | Upstream `main` plus all changes described here |
| `vibe/wayland-support-5f26ae` | Development branch with the same commits |
| `debug-diagnostics` | Same code **plus verbose autosplitter diagnostics** (10s heartbeat with read-failure stats, capped timer-call/loading-transition logging, PID-tagged log lines, attach info dump). Use this branch when a script misbehaves. |
| `appimage-test` | Ready-to-run AppImage binaries + README |

---

## 1. Wayland UI fix (no text / no UI rendered)

**Symptom:** under Wayland the window opened (rings visible) but no text or UI
elements rendered. Worked fine under X11.

**Root cause:** the `Window` constructor created the Silk.NET window and then
read back its size for ImGui. Under Wayland the size is not known at that
point, so ImGui captured a zero `DisplaySize` and `RenderImDrawData` early-
returned every frame — nothing was ever drawn.

**Fix:** `SpeedTool/Platform/Windows.cs` — the intended size is now passed
explicitly via `options.Size = sz;` *before* `SilkWindow.Create(options)`, so
ImGui always sees a valid display size.

## 2. Font discovery

**Problem:** upstream expected fonts at hardcoded paths that do not exist on
most Linux setups, which contributed to the "rings but no text" symptom.

**Fix:** `SpeedTool/Platform/Linux/Fonts.cs` now discovers fonts through
`fontconfig` (`fc-match`) and falls back through a list of common font files,
logging which font was chosen. `LoadFontEx` in `Windows.cs` was hardened with
fallbacks and logging.

## 3. Global hotkeys under Wayland (evdev backend)

**Problem:** X11 hotkey capture (SharpHook/libuiohook) does not work on
Wayland by design — clients cannot snoop global input.

**Fix:** new backend `SpeedTool/Platform/Linux/EvdevKeyboardHook.cs` reads
`/dev/input/event*` directly, which works on Wayland and X11 alike.
`KeyboardHook.cs` became a backend selector.

**Requirement:** the user needs read access to input devices:

```sh
sudo groupadd -r input 2>/dev/null || true
sudo usermod -aG input $USER
# log out and back in, then verify:
groups | grep input
```

To force a specific backend: `SPEEDTOOL_HOTKEY_BACKEND=evdev|sharpHook`.

## 4. Clipboard support (copy/paste everywhere)

**Problem:** Silk.NET's ImGuiController wires no clipboard callbacks, so
Ctrl+C/Ctrl+V in the script editor were silent no-ops.

**Fix:** `SpeedTool/Platform/GlfwClipboard.cs` wires ImGui's
`GetClipboardTextFn`/`SetClipboardTextFn` to the GLFW clipboard via
`[UnmanagedCallersOnly]` callbacks. Works on Wayland, X11 and Windows.

Additionally fixed a bug in `SpeedTool/ImGuiCodeEditor/TextEditor.cs`:
inserting multi-line text in the middle of a line duplicated the tail of the
split line.

## 5. Save/load dialog crash fixes

**Problems:** the file dialog started inside the read-only AppImage mount
(crash on save); double-clicking a directory invoked the open/save callback on
the first entry (crash); unauthorized locations crashed.

**Fix:** `SpeedTool/Platform/Linux/UniversalFileDialog.cs` /
`UniversalDirectory.cs` — dialogs now start in the home directory, navigation
never triggers callbacks, and errors are caught and displayed instead of
crashing.

## 6. AppImage support

`Program.cs` falls back to a writable config directory when running from a
read-only squashfs mount, so the AppImage works out of the box.

Build process (Linux, Native AOT):

```sh
dotnet publish SpeedTool/SpeedTool.csproj -c Release -r linux-x64
# copy SpeedTool/bin/Release/net8.0/linux-x64/publish/* into an AppDir
# (AppRun + speedtool.desktop + speedtool.png), then:
appimagetool AppDir SpeedTool-wayland-x86_64.AppImage
```

Ready-made binaries live on the `appimage-test` branch.

## 7. External autosplitter for Linux (loadless timing under Proton/Wine)

**Problem:** upstream's loadless timing uses DLL injection (EasyHook) into the
game process, which is Windows-only and does not work through Proton/Wine.

**Solution:** a *non-invasive* Linux autosplitter that never touches the game
process:

- `SpeedTool/Platform/Linux/LinuxProcesses.cs` — finds the game PID by
  scanning `/proc/*/cmdline` (and `/proc/pid/comm`), skipping SpeedTool itself.
- `SpeedTool/Platform/Linux/ProcMaps.cs` — parses `/proc/<pid>/maps` to find a
  module's base address (case-insensitive name match).
- `SpeedTool/Platform/Linux/ProcessMemoryReader.cs` — reads game memory via
  `/proc/<pid>/mem` (`pread64`). Failed reads return zeros to the script,
  matching upstream `ReadProcessMemory` behaviour.
- `SpeedTool/Platform/Linux/ExternalScriptEngine.cs` — runs the same Lua 5.4
  scripts as the Windows build under NLua, inside a properly enforced
  whitelist sandbox (see `Docs/SCRIPTING.md` for the API).
- `SpeedTool/Platform/Linux/GameTimeAccumulator.cs` — Stopwatch-based game
  clock that freezes while the script reports a loading state (same semantics
  as the injected Windows timer).
- `SpeedTool/Platform/Linux/ExternalAutosplitter.cs` — ties it together: a
  worker thread attaches/detaches, drives `on_frame` at ~250 Hz and applies
  script reloads. The worker is a background thread and owns the entire Lua
  engine lifecycle (fixes a crash when editing a script while hooked — the
  engine is never disposed cross-thread).

### Permission requirement (ptrace_scope)

Reading another process's memory requires ptrace access. On distributions with
Yama (`kernel.yama.ptrace_scope=1`) SpeedTool may only read its own children,
so run:

```sh
sudo sysctl kernel.yama.ptrace_scope=0
```

If attaching fails, the log prints this hint.

### 32-bit and 64-bit games

Pointer size is detected automatically from the game's PE header
(`machine` field at the module base): 32-bit games use 4-byte pointers,
64-bit games 8-byte. The log line `machine=0x014C (32-bit pointers)` confirms
the detection. This matters: dereferencing a 32-bit game with 8-byte reads
produces garbage and every script value stays at 0.

### Loadless timing semantics

Identical to the Windows build:

- The loadless timer only starts after the script calls `timer_start()`.
  Pressing split manually starts the run but the loadless clock stays at
  0:00.0 until the script fires.
- While the script reports a loading state (`timer_set_loading()`), elapsed
  time is discarded; on `timer_set_not_loading()` the clock resumes.
- `timer_split()` splits; Real Time mode is unaffected by the script.

### Logging (normal build)

The log (`~/.local/share/speedtool/debug.log`) contains one-time messages per
attach only: watching/attached lines, detected game bitness, script loaded
confirmation (`N chars, on_load=True, on_frame=True`), script errors, and the
ptrace hint on failure. For verbose diagnostics use the `debug-diagnostics`
branch.

## 8. Automatic game loading

`SpeedTool/Platform/GameWatcher.cs` adds optional auto-loading of game profiles:

- Every game profile loaded from a file (drag & drop, file dialog, save from
  the editor) is remembered in `GameWatcherSettings.KnownGames`.
- When enabled, a background watcher checks once per second whether a known
  game's exe is running and, if so, loads its profile automatically.
- Policy: only when no game is loaded; a game the user explicitly unloaded
  stays unloaded until its process exits and a **new** instance starts (the
  watcher tracks the process id, so a relaunch re-arms the timer).
- Toggle: right-click menu → *Auto-load game on launch* (persisted in the
  configuration).

On Linux this reuses the same `/proc` process scan as the external
autosplitter (`LinuxProcesses.FindByExeName`), so Proton games are detected
reliably; on Windows it uses `Process.GetProcessesByName`.

### Attach to process (no profile needed)

The right-click menu has an *Attach to process...* entry that opens a picker
with a dropdown of all running programs. For a picked process it can:

- **Load profile** — load an existing known profile for that exe.
- **Create profile** — immediately load a working stub profile (Real Time,
  one default split) pre-filled with the exe name and open the editor, so
  the timer is usable right away; saving registers it for future auto-load.
- **Ignore** — stop suggesting this process for this session.

Useful for games with long exe names (typical Unreal Engine naming like
`MyGame-Win64-Shipping.exe`): no typing required, pick it from the list.

When auto-load is enabled and an **unknown** `.exe` process starts, the
picker opens automatically (once per exe per session) — known profiles still
load silently as before, and ignored processes never prompt.

---

## 9. Known differences / notes for review

- **Lua sandbox:** upstream's `run_sandbox` helper sets `_ENV` only *inside*
  the helper, so scripts loaded by the injected Windows engine actually run
  with full global access (`os.execute` works). The Linux engine loads the
  script chunk with `load(chunk, name, 't', sandbox_env)`, which really
  enforces the whitelist. Worth fixing in `InjectedTimer` upstream.
- **Injection / DX10-DX12 hooks:** not implemented here. Windows injection
  remains untouched; DX10/DX12 overlay hooks (upstream issues #95/#97) are
  feasible future work.
- The autosplitter polls memory at ~250 Hz from one thread; all Lua state is
  owned by that thread.
