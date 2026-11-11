SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ ae8bd2d (vibe/wayland-support-5f26ae @ 9bab2d0)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- 32-bit game support: the autosplitter now detects the game's bitness from its
  PE header and dereferences pointers with the correct width (4 or 8 bytes).
  32-bit games (e.g. NFS13.exe) previously read garbage pointers, so every
  script value stayed at 0. The log now shows the detected bitness, e.g.
  "machine=0x014C (32-bit pointers)".

Previous features:
- Zombie-process fix: background autosplitter worker, PID-tagged log lines,
  readFailures/lastFail in the 10s heartbeat.
- debug_message(text) logged once per distinct text (value tracing).
- Autosplitter diagnostics: timer_start / timer_split calls, loading-state
  transitions, heartbeat.
- System clipboard support; multi-line paste fix; script-edit-while-hooked fix.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
