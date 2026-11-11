SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ a421507 (vibe/wayland-support-5f26ae @ 7c2f228)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- Autosplitter diagnostics: the debug log now records script timer calls
  (timer_start / timer_split), loading-state transitions and a heartbeat
  every 10s with the current loading/started/time state. Use this to see
  whether a loadless script actually fires.
- System clipboard support (copy/paste works everywhere, incl. Wayland).
- Fixed multi-line paste duplicating text in the script editor.
- Fixed crash when editing a game's script while the game is hooked.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
