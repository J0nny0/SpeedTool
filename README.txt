SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ 5f53973 (vibe/wayland-support-5f26ae @ def2284)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- Fixed crash when editing a game's script while the game is hooked:
  the Lua engine is now created and disposed only on the autosplitter
  worker thread; the UI thread only stages the new script.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. Same scripts and .stg game files as Windows. If
  attaching fails, run: sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
