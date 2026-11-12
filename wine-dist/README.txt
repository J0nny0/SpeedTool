Windows injector stack for the Wine/Proton injection experiment
(built from this branch: TimerInjector + InjectedTimer + EasyHook + NLua)

Usage: see Docs/WINE-INJECTION.md in the repository root.

Quick start:
1. Run SpeedTool (build from this branch) with SPEEDTOOL_TCP_PORT=47774 set.
2. Copy this folder somewhere permanent, e.g. ~/SpeedToolInject
   (Wine sees it as Z:\SpeedToolInject).
3. In the game's Wine prefix: winetricks dotnet48 (EasyHook needs the real CLR).
4. Start the game, then inject:
   wine Z:\SpeedToolInject\TimerInjector.exe NFS13.exe
   (accepts a Windows PID or a process name)
5. lua54.dll here is 32-bit — matches 32-bit games like NFS13.exe. If the
   injected script can't find Lua, copy lua54.dll into the game's directory.
