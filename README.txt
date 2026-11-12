SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: vibe/wayland-support-5f26ae @ e41ba17 (NOT merged to main yet)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- Automatic game loading: enable 'Auto-load game on launch' in the
  right-click menu; SpeedTool then loads a known game's profile
  automatically when its exe starts (works with Proton games). Every game
  file you load or save is remembered.
- Diagnostics trimmed for normal use. The log now only contains one-time
  messages per attach (watching/attached, detected game bitness, script
  loaded, script errors, ptrace hint on failure). No more 10s heartbeats
  or per-call logging.
- For verbose diagnostics (heartbeats with read-failure stats, capped
  timer-call/loading-transition logging, PID-tagged lines), use the
  debug-diagnostics branch of the fork instead.

Features in this line:
- 32-bit game support: pointer size detected from the game's PE header
  (e.g. NFS13.exe = 32-bit), so loadless scripts work for both widths.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- System clipboard support; multi-line paste fix; script-edit-while-hooked fix.
- Zombie-process fix: the autosplitter worker is a background thread, so
  closing all windows fully exits the process.
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log

## Experiment builds (test branches, not part of main)

- SpeedTool-pacing2ms-x86_64.AppImage — experiment/pacing-2ms @ ba5b9b0:
  the external autosplitter polls the game at 500 Hz instead of 250 Hz
  (worst-case transition error ~2 ms instead of ~4 ms). Drop-in replacement,
  nothing to configure.
- SpeedTool-injection-x86_64.AppImage — experiment/injection-wine @ 8c389ed:
  experimental Wine/Proton injection over TCP. Launch with
  `SPEEDTOOL_TCP_PORT=47774 ./SpeedTool-injection-x86_64.AppImage`, then run
  the injector from wine-dist/ (on the experiment/injection-wine branch)
  inside the game's prefix. Full instructions:
  Docs/WINE-INJECTION.md on that branch. Without the env var this build
  behaves exactly like the main one.
