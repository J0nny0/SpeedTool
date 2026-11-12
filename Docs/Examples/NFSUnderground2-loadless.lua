-- Need for Speed: Underground 2 -> SpeedTool
-- Ported from the LiveSplit ASL (by TheoFat?/community).
--
-- ASL settings are not available in SpeedTool. Behavior below matches the
-- ASL defaults: fullrunstart/shopsplit/racesplit/starsplit = ON,
-- stagestart/stagesplit = OFF. Flip the flags at the top to change this.
--
-- Notes:
--   * exit block (IsGameTimePaused) dropped - no equivalent in SpeedTool.

local FULLRUN_START = true
local STAGE_START = false
local SHOP_SPLIT = true
local RACE_SPLIT = true
local STAR_SPLIT = true
local STAGE_SPLIT = false

local STAGE_END_FMVS = {
    Scene07 = true,
    SUBURBAN = true,
    INDUSTEAST = true,
    INDUSTWEST = true,
    Scene15 = true,
}

function on_load()
    speed2 = module_base_address('SPEED2.exe')

    oldFmvPlaying = 0
    oldSmsMenu = 0
    oldInAShop = 0
    oldRaceState = 0
    oldStarEvent = 0
end

function on_frame()
    fmvName = read_ascii(speed2 + 0x4382A0, 10)
    raceState = read_int(speed2 + 0x49CE00)
    fmvPlaying = read_int(speed2 + 0x4383AC)
    loadingScreen = read_int(speed2 + 0x432E58)
    smsMenu = read_int(speed2 + 0x43632C)
    inAShop = read_int(speed2 + 0x427EC0)
    starEvent = read_int(speed2 + 0x438578)

    if FULLRUN_START and fmvName == "scene01" and fmvPlaying == 1 and oldFmvPlaying == 0 then
        timer_start()
    end
    if STAGE_START and smsMenu == 0 and oldSmsMenu == 2 then
        timer_start()
    end

    if loadingScreen == 0 and fmvPlaying == 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    local doSplit = false
    if STAGE_SPLIT and STAGE_END_FMVS[fmvName] and fmvPlaying == 1 and oldFmvPlaying == 0 then
        doSplit = true
    elseif SHOP_SPLIT and oldInAShop == 0 and inAShop == 636674500 then
        doSplit = true
    elseif RACE_SPLIT and raceState == 3 and oldRaceState == 1 then
        doSplit = true
    elseif RACE_SPLIT and raceState == 3 and oldRaceState == 2 then
        doSplit = true
    elseif STAR_SPLIT and oldStarEvent == 2 and starEvent == 0 then
        doSplit = true
    end
    if doSplit then
        timer_split()
    end

    oldFmvPlaying = fmvPlaying
    oldSmsMenu = smsMenu
    oldInAShop = inAShop
    oldRaceState = raceState
    oldStarEvent = starEvent
end
