SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ aa0ec03 (vibe/wayland-support-5f26ae @ 03bfa40)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- Zombie-process fix: the autosplitter worker is now a background thread, so
  closing all SpeedTool windows fully exits the process. Old builds could leave
  a hidden SpeedTool process running with an autosplitter still attached to the
  game. Before testing, check for strays with:  pgrep -af SpeedTool
- Log lines now include the SpeedTool PID (attach + heartbeat), so you can tell
  which instance produced which log lines.
- Heartbeat now shows readFailures=<count> lastFail=0x<addr>: memory reads
  failing inside the script (e.g. pointer derefs hitting unmapped memory).
- debug_message(text) from a script is logged once per distinct text, so a
  script can print per-frame values without flooding the log.

Previous features:
- Autosplitter diagnostics: timer_start / timer_split calls, loading-state
  transitions, 10s heartbeat.
- System clipboard support (copy/paste works everywhere, incl. Wayland).
- Fixed multi-line paste duplicating text in the script editor.
- Fixed crash when editing a game's script while the game is hooked.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
