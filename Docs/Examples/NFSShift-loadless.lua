-- Need for Speed: Shift -> SpeedTool
-- Ported from the LiveSplit ASL by Balathruin and WillTreaty.
--
-- Settings flags below mirror the ASL defaults.
-- gameTime/timing-method handling dropped (no equivalent in SpeedTool).

local FINISH_SPLIT = true
local MYCARS_SPLIT = false
local CARLOT_SPLIT = false
local UPGRADE_SPLIT = true
local TUNING_SPLIT = true
local QUITDRIFT_SPLIT = true

function on_load()
    shift = module_base_address('SHIFT.exe')

    oldState = 0
    oldFinish = 0
end

function on_frame()
    loading = read_int(pointer_path(shift + 0x008CA2D0, 0xD04, 0x2C))
    state = read_int(pointer_path(shift + 0x5F05C0, 0x0))
    finish = read_int(pointer_path(shift + 0x2A6020, 0xD3C))
    driftMode = read_ascii(pointer_path(shift + 0x324EBC, 0x27C), 25)

    if oldState == 73 and state == 74 then
        timer_start()
    end

    if loading == 34078832 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    local doSplit = false
    if FINISH_SPLIT and finish > oldFinish and loading ~= 34078832 then
        doSplit = true
    elseif MYCARS_SPLIT and oldState == 124 and state == 73 then
        doSplit = true
    elseif CARLOT_SPLIT and oldState == 110 and state == 73 then
        doSplit = true
    elseif UPGRADE_SPLIT and oldState == 118 and state == 73 then
        doSplit = true
    elseif TUNING_SPLIT and (oldState == 131 or oldState == 114) and state == 73 then
        doSplit = true
    elseif QUITDRIFT_SPLIT and driftMode == "Drift Mode:ON" and oldState == 63 and state == 65 then
        doSplit = true
    end
    if doSplit then
        timer_split()
    end

    oldState = state
    oldFinish = finish
end
