# SpeedTool code tour (read this before reading the code)

This is a guided tour through SpeedTool's source, written for someone reading
C# and this project for the first time. It explains what each part does, why
it looks the way it does, and how the pieces fit together. **No code was
changed to write this document** — everything here describes the code exactly
as it is on this branch.

---

## 0. The 20-second version

SpeedTool is a desktop speedrun timer. When it starts it:

1. Loads your settings from a JSON file,
2. Opens one main window,
3. Runs an endless loop: *for every open window, draw its UI once per frame*,
4. Reacts to hotkeys, game events and scripts that control start/split/reset,
5. Saves your run times as "PB" JSON files (PB = personal best).

Everything else — windows, timing maths, Linux support — hangs off that loop.

```
Program.Main()
   └─ Platform.SharedPlatform.Run()          ← the loop never ends until all windows close
        └─ for each Window: window.Cycle()   ← one "frame"
             ├─ window.Update()  (window events)
             ├─ OnUI()           (build the ImGui interface)
             └─ Render + swap    (show it on screen)
```

---

## 1. Start-up story — `SpeedTool/Program.cs`

Top-level statements (no `class` around them): each line of `Main` runs in
order when the app starts.

| Line | What it does |
|---|---|
| `GetConfigPath()` | Decides where `appsettings.json` lives. Next to the exe if that folder is writable (normal install), otherwise in `~/.local/share/speedtool/` (AppImages are read-only). |
| `AppDomain.CurrentDomain.UnhandledException += ...` | Safety net #1: if any exception kills the process from anywhere, write the full stack trace to the debug log first. |
| `NativeCrashCapture.Install()` | Safety net #2: some native crashes (abort / illegal instruction / bus error) skip all C# handlers. This installs raw POSIX signal handlers that write a literal `NATIVE CRASH: signal N` line to the log — the only thing that survives such a crash. |
| `Configuration.Init(...)` | Loads the JSON settings file. |
| `AddWindow(new MainWindow())` | Creates the main window. |
| `Platform.SharedPlatform.Run()` | The loop. Blocks *forever* (until the window closes). |
| `Release()` | Cleanup on the way out. |

The `NativeCrashCapture` class sits right under `Main` in the same file. It
works by pre-opening a file descriptor to the log and, inside a signal
handler, writing a pre-rendered message with plain libc `write()`. That trick
matters: inside a native crash you cannot safely use C# objects, memory
allocation, or file streams — only async-signal-safe libc calls.

---

## 2. The heart — `SpeedTool/Platform/Platform.cs`

`Platform` is a **singleton** ("there is exactly one, ever"). Its static field
`SharedPlatform` is used everywhere, so anything can reach the app state:
`Platform.SharedPlatform.Game`, `.LoadGame(...)`, `.AddWindow(...)`.

What it owns (the fields at the bottom of the class):

| Field | Meaning |
|---|---|
| `windows` | Every open window (main, editors, picker...). |
| `game` | The currently loaded game profile, or `null`. |
| `run` | The run currently on the clock (uses `game` + a category + the PB). |
| `hook` | Keyboard listener (the Wayland-capable hotkey system). |
| `hotkeyController` | Maps key combos → actions (split, reset, start...). |
| `injector` | The "loadless timing" source; see section 6. |
| `sources[]` | One timer source per timing method (real time, loadless, ...). |
| `pendingWindows` | A thread-safe queue of windows waiting to be opened. |

The most important methods:

- **`Run()`** — the loop from section 0. Two things to notice:
  - Each window's work is wrapped in `try/catch`. If one window throws, the
    exception is logged and that window is closed — the rest of the app keeps
    running. (Before this existed, one bad popup killed the whole timer.)
  - After the loop, windows are drained from `pendingWindows` and counted.
    That's how windows created by *other* windows get opened safely.
- **`LoadGame(Game game)` / `UnloadGame()`** — remember the game, reset the
  timing category to the first one, rebuild the run. `ReloadRun()` is the
  common piece: it also (re)creates the timing source.
- **`GetPBRun(...)` / `SaveRunAsPB(...)`** — read/write PB files in
  `~/.local/share/speedtool/pbs/`, named `GameName.CategoryName.json`.
  Reading is wrapped in try/catch: a corrupt PB file is deleted (logged) and
  treated as "no PB yet", never as a crash.
- **`AddWindow(Window w)`** — never opens the window immediately, pushes it
  onto `pendingWindows` instead, so it can be called from any thread and from
  inside UI code.

---

## 3. Windows and drawing — `SpeedTool/Platform/Windows.cs`

`Window` is the base class every program window inherits. It wraps a
[Silk.NET](https://github.com/dotnet/Silk.NET) `IWindow`, which in turn talks
to **GLFW** — the library that creates OS windows and OpenGL contexts. On
Linux GLFW can use **Wayland or X11**, and that single switch is what most of
the Wayland work revolved around.

A window has three jobs per frame:

1. **`Update()`** — poll window events (resize, close, input).
2. **`OnUI(double dt)`** — build the interface with **ImGui** calls
   (`ImGui.Begin`, `ImGui.Text`, ...). ImGui is "immediate mode": instead of
   creating buttons as objects once, you re-describe the whole UI every frame.
   `if (ImGui.Button("Save")) { SaveNow(); }` — the call *is* the button.
3. **Render** — submit the ImGui draw data to OpenGL and swap buffers.

Two rules explained (they look strange until you know why):

- **Every window has its own ImGui context.** The base class creates one per
  window (`ImGuiController.Init` → `CreateContext` + `SetCurrentContext`).
  That gives each window isolated state (fonts, style, input) but it also
  means code must never mix two contexts mid-frame — that's why…
- **Never open or close a window from inside `OnUI`.** Mid-frame window
  creation corrupts ImGui's internal state. Anything that needs a new window
  (a button click, the process picker, an "error" popup) defers it:
  `onExit = () => platform.AddWindow(...)` runs *after* `ImGui.End()`, or
  `AddWindow` queues the window for the next `Run()` pass.

Also in this file: the Wayland **blank-UI fix**
(`options.Size = sz` in the constructor — a GLFW quirk: without an explicit
size, Wayland compositors gave the window a zero-size surface, and ImGui drew
into nothing), font loading (`LoadFontEx`), and the one-time "first frame"
log markers used to debug where a window dies.

Two log lines help you map a window class to what you see:

```csharp
// Windows.cs, end of Window constructor — session + window type + backend:
DebugLog.SharedInstance.Write($"Window {GetType().Name}: session " +
    $"WAYLAND_DISPLAY='{...}' framebuffer={w}x{h} ...");
// Platform.cs, AddWindow — who was opened:
DebugLog.SharedInstance.Write($"Window added: {w.GetType().Name}");
```

---

## 4. How times are stored — `SpeedTool/Splits/` + `SpeedTool/Timer/`

This folder holds the *data model* (no UI, no platform code):

| File | Meaning |
|---|---|
| `Game.cs` | One game profile: name, exe name, default timing, script, categories. Loads/saves `.stg` files (zip, see section 8). |
| `Category.cs` | A speedrun category ("any%", "100%") with its `Split` list. |
| `Split.cs` | One split; may have subsplits. |
| `TimeCollection.cs` | A fixed set of numbers — one time per **timing method** (`TimingMethod.cs` enumerates them; the converter reads exactly `TIMES_COUNT` values, so this array shape is part of the PB file format). |
| `SplitsController.cs` | Flattens splits (top level + subsplits) into one list, tracks the current split index. |
| `RunInfo.cs` / `SplitInfo.cs` | The **serialized** (JSON) forms of a run — what goes into PB files. |
| `RunCollection.cs` | Older helper, currently not on a hot path. |

And the raw pieces:

- `Timer/TimingMethod.cs` — the enum of timing methods (e.g. RealTime,
  Loadless, ...). `TimeRefFor(method)` returns a `ref` so UI code can edit
  one number in the collection directly.
- `Util/Time.cs` + `Util/TimeStringParser.cs` — turn `1:23.4` text into
  `TimeSpan` and back. The parser never throws; invalid input becomes
  `TimeSpan.MaxValue` and editors treat that as "empty".

**The flow of a live run:** the active timing source (section 6) produces
`CurrentTime` each frame → `Run`/`SplitsController` decide whether a split
should trigger → times are copied into the current `Split`'s
`TimeCollection` → the timer UI just reads and formats them.

**PB files:** `Platform.SaveRunAsPB` writes
`pbs/<Game>.<Category>.json` (serialized `RunInfo` via JSON source
generation). `GetPBRun` reads it when a run starts so the comparison column
can show your best.

---

## 5. Windows that exist — `SpeedTool/Windows/`

| File | What you see | Notes for readers |
|---|---|---|
| `MainWindow.cs` | The timer + right-click menu. | Also handles profile drag&drop (`OnFilesDropped` — profile files only; that's where the "Broken file" dialog comes from) and the env-gated self-test. |
| `GameEditorWindow.cs` | Create/edit a game profile. | `OnClosing` → `LoadGame(CollectGame())`, so edits apply immediately. |
| `TimeEditorWindow.cs` | Edit saved times ("Edit Times"). | Loads a PB-ish `RunInfo`, shows one editable time per split, saves via `SaveRunAsPB`. All data was hardened (`?? empty` guards) because bad PB files used to crash here. |
| `CodeEditorWindow.cs` | Lua script editor. | Wraps `ImGuiCodeEditor/` (a third-party ImGui text editor component). |
| `ProcessPickerWindow.cs` | "Attach to process..." picker. | Every button defers its action via `afterUi` (section 3's rule). |
| `SettingsWindow.cs` + `Settings/Tabs/*` | Settings. | One tab class per settings section. |
| `AboutWindow.cs`, `DebugMessagesWindow.cs` | Small info windows. | |
| `TimerUI/*` | The actual timer faces (`Classic`, `SpeedTool`). | Pure ImGui drawing, no logic. |

---

## 6. "Where does the timer's time come from?" — `SpeedTool/Timer/` + timing sources

`ITimerSource` is the tiny interface every time source implements
(`CurrentTime`, `CurrentState` ...). `IAutosplitter` adds split/game events.
Implementations:

| Source | File | When it's used |
|---|---|---|
| `BasicTimer` | `Timer/BasicTimer.cs` | Plain real-time when no game is attached — a stopwatch. |
| `NoTimer` | `Timer/NoTimer.cs` | Placeholder that always reports 0. |
| **InjectorHandler** | `Injector/InjectorHandler.cs` | Windows-style injected timing, and on Linux the **injection experiment**: SpeedTool *listens* on TCP `127.0.0.1:47774` and a DLL inside the game sends messages. |
| **ExternalAutosplitter** | `Platform/Linux/ExternalAutosplitter.cs` | **The Linux loadless timing that ships here** — reads the game's memory directly. |

The `Injector/` folder (host side):

- `InjectorHandler.cs` — owns the connection; on Linux it starts a TCP
  listener (via `Pipe`), then a 50 ms worker loop parses incoming lines.
- `Pipe.cs` — the transport: named pipe on Windows, TCP on Linux
  (`UseTcp`/`TcpPort`). Line protocol (both sides): `script <file>`,
  `script_load`, `start`, `reset` → `timer <ticks>`, `split`,
  `debug_message <text>`.
- `PipeReader.cs` — splits a TCP stream into lines (TCP does not preserve
  message boundaries — that's the whole problem this class solves).

The Linux external autosplitter (`Platform/Linux/`), how a layman can picture
it — a small robot with a loop:

```
every 4 ms:
  1. find the game process under /proc  (LinuxProcesses.cs, ProcessList.cs)
  2. find where its EXE/DLLs sit in memory (ProcMaps.cs)
  3. read the memory addresses the Lua script asked for (ProcessMemoryReader.cs)
  4. run those values through the Lua script   (ExternalScriptEngine.cs)
  5. whatever the script reports (start/split/timer ticks) → the timer
     and GameTimeAccumulator stitch the values into a loadless clock
```

- **`ExternalAutosplitter.cs`** — the loop itself: process discovery, attach,
  bitness check (reads the PE header of the game's exe — 32 vs 64 bit),
  worker-thread pacing, reconnect on game exit.
- **`ProcMaps.cs`** — parses `/proc/<pid>/maps` (the OS's memory map file) to
  turn "module name" into "memory address".
- **`ProcessMemoryReader.cs`** — opens `/proc/<pid>/mem` and reads bytes at
  addresses. This needs kernel permission (`ptrace_scope`); if the log says
  "permission denied", that's this.
- **`ExternalScriptEngine.cs`** — a sandboxed Lua interpreter (NLua). The
  game profile's `script.lua` declares watch addresses and functions
  (`update`/`start`/`split`/...). The sandbox removes Lua functions that
  could touch the disk, so a profile can't do more than read memory and
  report events.
- **`GameTimeAccumulator.cs`** — turns frame ticks into wall-clock-safe
  loadless time.

---

## 7. Hotkeys + input — `SpeedTool/Platform/Keyboard*.cs`

- `KeyboardHook.cs` picks the backend at startup on Linux:
  **X11** (if an X server is around) or the **evdev** backend
  (`Platform/Linux/EvdevKeyboardHook.cs`), which reads the raw input devices
  in `/dev/input` — that's why the app needs the `input` group under Wayland.
- `Keyboard.cs` / `KeyPressData.cs` — normalized key state feeding the UI.
- `Hotkey.cs` + `HotkeyController.cs` — a hotkey = a key combo + an action;
  `HotkeyController` matches the live state against configured combos and
  calls the action (split, start, reset...). Combos live in settings
  (`HotkeySettings`).

---

## 8. Profiles and settings on disk

Two file kinds — don't mix them up (this bit us once):

| Kind | Where | Content |
|---|---|---|
| **Game profile** `.stg` | anywhere (you drag it onto the window) | A **zip** with `meta.json`, optional `script.lua`, `categories/<name>.json` — see `Game.cs` `SaveToFile`/`LoadFromFile`. |
| **PB file** `.json` | `~/.local/share/speedtool/pbs/` | Serialized `RunInfo` — created automatically when a run finishes; *not* drag&drop-loadable (dropping a `.json` = "Broken file" by design). |

Settings: `Global/Configuration.cs` loads/saves `appsettings.json`;
`Global/Definitions/*.cs` are one small class per settings section
(colors, hotkeys, UI, watcher...). Settings classes are JSON-serialized with
**source generation** — see `JSON/SourceGeneratorContext.cs`: every type that
may be serialized must be listed there, because the app compiles to **native
code (AOT)**, where C# reflection does not exist. The same applies to
`RunInfo`: `JSON/TimeCollectionConverter.cs` is a custom converter for the
fixed-size time array.

---

## 9. The rest of `Platform/Linux/` — plugging Windows-shaped holes

| File | Windows has... | Linux version does... |
|---|---|---|
| `Fonts.cs` | ships system fonts | asks **fontconfig** for real installed fonts |
| `UniversalFileDialog.cs` / `DialogOperation.cs` | native dialogs | opens an X11 dialog window (works under both backends) |
| `GlfwClipboard.cs` (in `Platform/`) | clipboard API gaps | routes copy/paste through GLFW's Wayland/X11 clipboard |
| `UniversalDirectory.cs` | `Directory` wrappers | ref-counted temp dirs |
| `GameWatcher.cs` | — | watches `/proc` for a game exe and can auto-load its profile (toggle in the main menu) |

---

## 10. Debugging — how to read `~/.local/share/speedtool/debug.log`

- `DebugLog.cs` (`Platform/Debugging/`) is the single logging class
  (`DebugLog.SharedInstance.Write(...)` appends one timestamped line).
  It **rotates**: over ~2 MB, old content moves to `debug.old.log`.
- Useful line shapes:
  - `Keyboard hook: using ... backend` — one per app start = session border.
  - `Window: session ...` / `Window added: <Type>` — window lifecycle.
  - `Loading game <name>` / `Unloading game <name>` — profile switches.
  - `first frame: update / ui done / render done` — one-time frame-phase
    markers per window; if a crash log stops between them, you know the
    exact phase that died.
  - `NATIVE CRASH: signal N` — the last line when a native crash happens.
  - `Window <X> threw an exception, closing it` — contained error, app
    survives; the exception follows.
- `SPEEDTOOL_AUTO_TEST=1` (env var) enables a headless self-test in
  `MainWindow.AutoTestTick()`: loads a stub game, then opens the timing
  editor mid-frame, to reproduce bugs without a real game.

---

## 11. Symptom → where to look

| You see... | First look at |
|---|---|
| Blank window / no UI on Wayland | `Windows.cs` (`options.Size = sz` in the ctor) |
| Hotkeys dead | `KeyboardHook.cs` (backend pick), `EvdevKeyboardHook.cs` (input group), `HotkeyController.cs` |
| Loadless timer never moves | `ExternalAutosplitter.cs` (attach lines in the log), `ProcessMemoryReader.cs` (ptrace_scope!), `ExternalScriptEngine.cs` |
| Loadless on the injection branch | `InjectorHandler.cs`, `Pipe.cs` (TCP), `Docs/WINE-INJECTION.md` |
| "Broken file" when dropping a profile | `MainWindow.OnFilesDropped` → `Game.LoadFromFile` (see also section 8) |
| Crash while saving/editing times | `TimeEditorWindow.cs`, `TimeCollectionConverter.cs`, the PB path in `Platform.cs` |
| Wrong font / no font | `Fonts.cs` |
| Game doesn't auto-load | `GameWatcher.cs` (+ its settings class) |
| Weird crash with no message | `Program.cs`'s two safety nets + the frame-phase markers |

## 12. Glossary

- **frame** — one iteration of the loop in `Platform.Run()`; ~60 per second
  (each window draws once per frame).
- **ImGui context** — an isolated bundle of ImGui state (fonts, style,
  per-window input). Each `Window` owns exactly one.
- **PB / RunInfo** — a saved personal best; the JSON form of a finished run.
- **loadless** — a timing method that measures only in-game time, invisible
  loading screens excluded; needs game-internal hooks or memory reads.
- **AOT (Native AOT)** — the app is compiled ahead-of-time to a native
  Linux binary; no JIT, no reflection. Consequence: all JSON types must be
  pre-declared in `SourceGeneratorContext`.
- **GLFW** — the windowing/input library beneath Silk.NET; picks
  X11 or Wayland at runtime.
- **evdev** — Linux kernel's raw input device interface; the Wayland-safe
  way to see global key presses.
- **protontricks / winetricks** — tools that tweak a game's Proton (Wine)
  prefix, e.g. to install .NET for the injection experiment.

---

*End of tour. If something here disagrees with the code, the code wins — and
the file names above are always your fastest way in.*
