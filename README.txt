SpeedTool Wayland + Linux autosplitter build (x86_64)
Built from: J0nny0/SpeedTool main @ 2361866 (vibe/wayland-support-5f26ae @ 89c3287)

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

New in this build:
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
