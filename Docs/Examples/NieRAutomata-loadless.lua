-- NieR: Automata -> SpeedTool
-- Ported from the LiveSplit ASL (by fakerererer0055 et al.).
--
-- The ASL detects the game version by module memory size, which SpeedTool
-- cannot do. Set VERSION to "1.0", "1.01", "1.02" or "BAGS" below.
-- Steam release is "1.02"; "BAGS" is the Become As Gods Edition (Xbox/Win10).
--
-- Notes:
--   * Only cutscene-based splits are implemented (same as the ASL).

local VERSION = "1.02"

local OFF = {
    ["1.0"] = {
        isWorldLoaded       = 0x18F3978,
        playerNameSetStatus = 0x1461B38,
        currentCutscene0    = 0x1978868, currentCutsceneOff = 0x1F4,
        isCutscenePlaying   = 0x146A1AC,
        isLoading           = 0x18DD444,
    },
    ["1.01"] = {
        isWorldLoaded       = 0x110ADC0,
        playerNameSetStatus = 0x147B4BC,
        currentCutscene0    = 0x19925E8, currentCutsceneOff = 0x1F4,
        isCutscenePlaying   = 0x1483974,
        isLoading           = 0x147BF50,
    },
    ["BAGS"] = {
        isWorldLoaded       = 0xFE7F20,
        playerNameSetStatus = 0x12CE5D4,
        currentCutscene0    = 0x17FD648, currentCutsceneOff = 0x294,
        isCutscenePlaying   = 0xFA54E8,
        isLoading           = 0x11435C0,
    },
    ["1.02"] = {
        isWorldLoaded       = 0xF6E240,
        playerNameSetStatus = 0x124DE4C,
        currentCutscene0    = 0x17805F0, currentCutsceneOff = 0x294,
        isCutscenePlaying   = 0x102A244,
        isLoading           = 0x14005F4,
    },
}

local CUTSCENES = {
    "movie/ev0010.usm", "movie/ev0030.usm", "movie/ev0060.usm",
    "movie/ev0070.usm", "movie/ev0080.usm", "movie/ev0090.usm",
    "movie/ev0120.usm", "movie/ev0140.usm",
    "movie/ev0200.usm", "movie/ev0210.usm", "movie/ev0220.usm",
    "movie/ev0240.usm", "movie/ev0250.usm", "movie/ev0260.usm",
    "movie/ev0300.usm", "movie/ev0310.usm", "movie/ev0320.usm",
    "movie/ev0325.usm", "movie/ev0330.usm", "movie/ev0350.usm",
    "movie/ev0370.usm", "movie/ev0380.usm", "movie/ev0390.usm",
    "movie/ev0400.usm",
    "movie/ev0430.usm", "movie/ev0440.usm", "movie/ev0450.usm",
    "movie/ev0470.usm", "movie/ev0482.usm",
    "movie/ev0510.usm", "movie/ev0520.usm", "movie/ev0525.usm",
    "movie/ev0530.usm", "movie/ev0550.usm",
    "movie/ev0570.usm", "movie/ev0580.usm", "movie/ev0610.usm",
    "movie/ev0630.usm", "movie/ev0642.usm", "movie/ev0655.usm",
    "movie/ev0660.usm", "movie/ev0670.usm", "movie/ev0693.usm",
    "movie/ev0262.usm", "movie/ev0322.usm", "movie/ev0352.usm",
    "movie/ev0483.usm", "movie/ev0552.usm", "movie/ev0650.usm",
    "movie/ev0694.usm",
    "movie/ev0830.usm", "movie/ev0875.usm", "movie/ev0940.usm",
    "movie/ev0970.usm", "movie/ev1010.usm", "movie/ev1060.usm",
    "movie/ev1125.usm", "movie/ev1190.usm", "movie/ev1210.usm",
    "movie/ev1232.usm", "movie/ev1233.usm", "movie/ev1234.usm",
    "movie/ev1235.usm", "movie/ev1250.usm",
    "movie/ev1270.usm",
}

local CUTSCENE_SET = {}
for _, name in ipairs(CUTSCENES) do
    CUTSCENE_SET[name] = true
end

function on_load()
    game = module_base_address('NieRAutomata.exe')
    o = OFF[VERSION]

    oldIsWorldLoaded = 0
    oldIsCutscenePlaying = 0
end

function on_frame()
    isWorldLoaded = read_int(game + o.isWorldLoaded) & 0xFF
    playerNameSetStatus = read_int(game + o.playerNameSetStatus) & 0xFF
    currentCutscene = read_ascii(pointer_path(game + o.currentCutscene0, o.currentCutsceneOff), 32)
    isCutscenePlaying = read_int(game + o.isCutscenePlaying) & 0xFF
    isLoading = read_int(game + o.isLoading) & 0xFF

    if playerNameSetStatus == 1 and isWorldLoaded ~= 0 and oldIsWorldLoaded == 0 then
        timer_start()
    end

    if isLoading ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    if isCutscenePlaying ~= 0 and oldIsCutscenePlaying == 0 and CUTSCENE_SET[currentCutscene] then
        timer_split()
    end

    oldIsWorldLoaded = isWorldLoaded
    oldIsCutscenePlaying = isCutscenePlaying
end
