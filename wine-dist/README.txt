Windows injector stack for SpeedTool injection under Wine/Proton
(built from this branch: TimerInjector + InjectedTimer + EasyHook + NLua)

Usage: see Docs/WINE-INJECTION.md in the repository root.

Quick start:
1. Run SpeedTool (AppImage built from this injection-only branch). No env var
   needed: TCP port 47774 is the default here (override: SPEEDTOOL_TCP_PORT).
2. Copy this folder somewhere permanent, e.g. ~/SpeedToolInject.
3. In the game's Wine prefix: winetricks dotnet48 (EasyHook needs the real CLR).
   NOTE: on new Proton versions, installing dotnet48 with Proton's own wine fails
   ("returned status 67"). Use system wine instead:
     WINEPREFIX=<prefix> WINE=(which wine) winetricks -q dotnet48
4. Start the game, then inject (32-bit injector on purpose: injector and target
   must share one architecture, otherwise EasyHook's WOW64 bypass hits an
   unsupported cross-bitness module read under Wine):
   wine ~/SpeedToolInject/TimerInjector.exe NFS13.exe
   (accepts a Windows PID or a process name; wine translates Unix paths)
5. Lua bitness (IMPORTANT - native dll, must match the game):
   - lua54.dll here is 32-bit -> for 32-bit games like NFS13.exe
   - lua54-x64.dll is 64-bit -> rename to lua54.dll (or copy into the game
     directory as lua54.dll) for 64-bit games
   If the injected script reports that Lua cannot be loaded, the bitness is
   the first thing to check.
