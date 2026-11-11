SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ c56d0f7 (vibe/wayland-support-5f26ae @ 4cfca86)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
- System clipboard support: copy/paste (Ctrl+C/Ctrl+V/Ctrl+X) now works in the
  script editor and all text fields, including pasting scripts from a browser.
  Previously paste silently did nothing.
- Fixed a code editor bug that duplicated text when pasting multi-line
  scripts in the middle of a line.
- Fixed crash when editing a game's script while the game is hooked.
- External Lua autosplitter on Linux: loadless timing for Proton/Wine games
  without injection. If attaching fails, run:
  sudo sysctl kernel.yama.ptrace_scope=0
- Wayland UI fix, evdev hotkey backend (input group), save-dialog crash fixes.

Logs: ~/.local/share/speedtool/debug.log
