SpeedTool Wayland diagnostic build (x86_64)
Built from: J0nny0/SpeedTool branch vibe/wayland-support-5f26ae @ f9c59a6

Run: chmod +x SpeedTool-wayland-x86_64.AppImage && ./SpeedTool-wayland-x86_64.AppImage

This build adds:
- Window created at its final size from the start (candidate fix for blank UI on Wayland)
- A white test square in the top-left corner of the window
- Per-frame diagnostics in ~/.local/share/speedtool/debug.log (also printed to console):
  "Frame N: windowSize=... fb=... displaySize=... cmdLists=... vtx=... idx=... texID=... glErr=..."
  "GL: vendor=... renderer=... version=..."
