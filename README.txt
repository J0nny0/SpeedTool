SpeedTool Wayland build (x86_64) - WORKING, save-dialog crash fix
Built from: J0nny0/SpeedTool branch vibe/wayland-support-5f26ae @ 1de0b0e

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

Fix: the window is now created at its final size from the start, which repairs
the blank UI (rings but no text) on Wayland. Diagnostic white square and
per-frame logging have been removed.

Hotkeys on Wayland use the evdev backend; your user needs to be in the
"input" group: sudo usermod -aG input $USER (then log out and back in).
Logs: ~/.local/share/speedtool/debug.log
