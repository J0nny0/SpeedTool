-- Need for Speed: Most Wanted (2005) -> SpeedTool
-- Ported from the LiveSplit ASL by TDOG20, Jigsaw, Balathruin and WillTreaty.
--
-- Set VERSION to "v1.2" or "v1.3". Steam release is v1.3.
--
-- NOT PORTED (SpeedTool has no timer-phase/split-index/reset access):
--   * Quick Race Mode entirely (interval splits, checkpoint splits, resets)
--   * "First Lap Glitches Split" and "Resume Career Split" (need split index)
--   * reset block, gameTime/timing-method handling
-- Settings flags below mirror the ASL defaults.

local VERSION = "v1.3"

local INTRO_SPLIT = true
local PROLOGUE_SPLIT = true
local SAFEHOUSE_SPLIT = true
local SHOP_SPLIT = true
local RACE_SPLIT = true
local CAMERA_SPLIT = true
local BOSS_SPLIT = false
local MARKER_SPLIT = true
local COPCHASE_SPLIT = true
local BUSTED_SPLIT = false
local GAMEOVER_SPLIT = false
local BRIDGEJUMP_SPLIT = true

local OFF = {
    v12 = {
        trackID0      = 0x51CFCC, trackIDPath = { 0x44, 0x4, 0x0 },
        selectedMenu  = 0x51BF84,
        start0        = 0x51BA9C, startPath = { 0x0, 0x18C },
        gameover0     = 0x5C1220, gameoverPath = { 0x490 },
        fmvName0      = 0x51C228, fmvNamePath = { 0x18, 0x68, 0x40, 0x0 },
        nisName0      = 0x50CA88, nisNamePath = { 0xC, 0x10, 0x4 },
        loadingScreen1 = 0x510ED0,
        loadingScreen2 = 0x5112D0,
        restartLoad   = 0x51BEE8,
        gamestateID   = 0x51CCB4,
        raceState     = 0x50F764,
        onChase       = 0x51C398,
        onRace        = 0x5B27E8,
        inShop0       = 0x51B120, inShopPath = { 0x118 },
    },
    v13 = {
        trackID0      = 0x51E00C, trackIDPath = { 0x48, 0x1C },
        selectedMenu  = 0x51CFC4,
        start0        = 0x0, startPath = {},
        gameover0     = 0x5C2270, gameoverPath = { 0x490 },
        fmvName0      = 0x51D268, fmvNamePath = { 0x18, 0x68, 0x40, 0x0 },
        nisName0      = 0x50DAC8, nisNamePath = { 0xC, 0x10, 0x4 },
        loadingScreen1 = 0x511F10,
        loadingScreen2 = 0x512310,
        restartLoad   = 0x51CF28,
        gamestateID   = 0x51DCF4,
        raceState     = 0x5107A4,
        onChase       = 0x51D3D8,
        onRace        = 0x5B3828,
        inShop0       = 0x51C160, inShopPath = { 0x118 },
    },
}

local LAST_BOSS_RACES = {
    ["15_1_1_circuit_reversed"] = true,
    ["15_2_1_sprint_reverse"] = true,
    ["14_2_2_r_sprint"] = true,
    ["12_1_2_circuit"] = true,
    ["12_1_2_r_circuit"] = true,
    ["10_7_3_drag"] = true,
    ["9_2_1_sprint"] = true,
    ["8_7_5_drag"] = true,
    ["7_2_3_p2p"] = true,
    ["6_5_2_speedtraprace"] = true,
    ["5_2_1_r_sprint"] = true,
    ["4_5_1_r_speedtrap"] = true,
    ["3_1_2_circuit"] = true,
    ["2_2_1_sprint"] = true,
    ["1_2_3_sprint"] = true,
}

function on_load()
    speed = module_base_address('speed.exe')
    o = OFF[VERSION == "v1.2" and "v12" or "v13"]

    oldStart = 0.0
    oldSelectedMenu = 0
    oldFmvName = ""
    oldNisName = ""
    oldInShop = 0
    oldRaceState = 0
    oldOnChase = 0
    oldLoadingScreen1 = 0
    oldLoadingScreen2 = 0
    oldGamestateID = 0
    oldGameover = 0
end

function on_frame()
    trackID = read_ascii(pointer_path(speed + o.trackID0, table.unpack(o.trackIDPath)), 30)
    selectedMenu = read_int(speed + o.selectedMenu)
    gameover = read_int(pointer_path(speed + o.gameover0, table.unpack(o.gameoverPath)))
    fmvName = read_ascii(pointer_path(speed + o.fmvName0, table.unpack(o.fmvNamePath)), 14)
    nisName = read_ascii(pointer_path(speed + o.nisName0, table.unpack(o.nisNamePath)), 11)
    loadingScreen1 = read_int(speed + o.loadingScreen1)
    loadingScreen2 = read_int(speed + o.loadingScreen2)
    restartLoad = read_int(speed + o.restartLoad) & 0xFF
    gamestateID = read_int(speed + o.gamestateID)
    raceState = read_int(speed + o.raceState)
    onChase = read_int(speed + o.onChase)
    onRace = read_int(speed + o.onRace)
    inShop = read_int(pointer_path(speed + o.inShop0, table.unpack(o.inShopPath)))

    local startVal = nil
    if VERSION == "v1.2" then
        startVal = read_float(pointer_path(speed + o.start0, table.unpack(o.startPath)))
    end

    if VERSION == "v1.2" and selectedMenu == 3
        and ((startVal == 60 and oldStart ~= startVal) or oldSelectedMenu ~= selectedMenu) then
        timer_start()
    end
    if (selectedMenu == 1 and oldInShop == 88 and inShop == 21)
        or (selectedMenu == 2 and loadingScreen2 == 24) then
        timer_start()
    end

    if loadingScreen1 == 32767 or loadingScreen2 == 24 or loadingScreen2 == 0 or gamestateID == 16 then
        timer_set_loading()
    elseif gamestateID == 32 then
        if loadingScreen2 == 34 then
            timer_set_not_loading()
        else
            timer_set_loading()
        end
    elseif onRace == 257 and restartLoad ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    local doSplit = false
    if INTRO_SPLIT and oldFmvName ~= fmvName and fmvName == "storyfmv_cro06" then
        doSplit = true
    elseif PROLOGUE_SPLIT and oldFmvName ~= fmvName and fmvName == "storyfmv_bus12" then
        doSplit = true
    elseif SAFEHOUSE_SPLIT and oldFmvName ~= fmvName and fmvName == "storyfmv_saf25" then
        doSplit = true
    elseif SHOP_SPLIT and (oldInShop == 111 or oldInShop == 72) and oldInShop ~= inShop then
        doSplit = true
    elseif RACE_SPLIT and raceState == 256 and oldRaceState == 0 and onRace == 257 then
        doSplit = true
    elseif CAMERA_SPLIT and raceState == 256 and oldRaceState == 0 and onRace ~= 257 then
        doSplit = true
    elseif BOSS_SPLIT and not RACE_SPLIT and raceState == 256 and oldRaceState == 0 and onRace == 257
        and (LAST_BOSS_RACES[trackID] or LAST_BOSS_RACES[string.sub(trackID, 1, #trackID - 4)]) then
        doSplit = true
    elseif MARKER_SPLIT and #oldFmvName > 0 and not string.find(oldFmvName, "blacklist_15", 1, true)
        and string.find(oldFmvName, "blacklist_", 1, true) and oldFmvName ~= fmvName then
        doSplit = true
    elseif COPCHASE_SPLIT and oldOnChase == 1 and onChase == 0 and onRace ~= 257
        and oldLoadingScreen1 ~= 32767 and oldLoadingScreen2 ~= 24 and oldLoadingScreen2 ~= 0
        and oldGamestateID ~= 16
        and trackID ~= "epic_pursuit" and trackID ~= "epic_pursuit.vlt" then
        doSplit = true
    elseif BUSTED_SPLIT and onChase == 1 and loadingScreen1 == 32767
        and oldLoadingScreen1 ~= loadingScreen1 then
        doSplit = true
    elseif GAMEOVER_SPLIT and gameover == 10 and oldGameover ~= gameover and inShop == 84 then
        doSplit = true
    elseif BRIDGEJUMP_SPLIT and oldNisName ~= nisName and nisName == "EndingNis04" then
        doSplit = true
    end
    if doSplit then
        timer_split()
    end

    if startVal ~= nil then oldStart = startVal end
    oldSelectedMenu = selectedMenu
    oldFmvName = fmvName
    oldNisName = nisName
    oldInShop = inShop
    oldRaceState = raceState
    oldOnChase = onChase
    oldLoadingScreen1 = loadingScreen1
    oldLoadingScreen2 = loadingScreen2
    oldGamestateID = gamestateID
    oldGameover = gameover
end
