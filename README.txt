SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ cac7c03 (vibe/wayland-support-5f26ae @ 3d595de)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games without injection.
  Same scripts and .stg game files as Windows. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
