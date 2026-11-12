-- Need for Speed: Undercover -> SpeedTool
-- Ported from the LiveSplit ASL by Failracer, TDOG20 and WillTreaty.
--
-- The ASL has ~70 per-event settings, all defaulting to ON. Here they are a
-- Lua table instead: set any entry to false to disable that event's split.
-- Set SPLIT_ALL = true to split on every event regardless of the table.
--
-- Notes:
--   * gameTime/timing-method handling dropped (no equivalent in SpeedTool).

local SPLIT_ALL = false

local ENABLED = {
    ["sp_c_12.vlt"] = true, ["hb_c_01.vlt"] = true, ["sp_c_21.vlt"] = true,
    ["or_c_03.vlt"] = true, ["sp_c_20.vlt"] = true, ["cr_c_03.vlt"] = true,
    ["hb_c_04.vlt"] = true, ["or_c_07.vlt"] = true, ["cts_c_02.vlt"] = true,
    ["es_c_02.vlt"] = true, ["cts_c_01.vlt"] = true, ["hb_c_03.vlt"] = true,
    ["cts_c_03.vlt"] = true, ["es_c_01.vlt"] = true, ["E047.vlt"] = true,
    ["or_c_01.vlt"] = true, ["E069.vlt"] = true, ["E071.vlt"] = true,
    ["es_c_03.vlt"] = true, ["cts_c_04.vlt"] = true, ["E120.vlt"] = true,
    ["es_c_04.vlt"] = true, ["cts_c_06.vlt"] = true, ["or_c_04.vlt"] = true,
    ["cts_c_05.vlt"] = true, ["E085.vlt"] = true, ["or_c_02.vlt"] = true,
    ["hb_c_02.vlt"] = true, ["E096.vlt"] = true, ["E098.vlt"] = true,
    ["E115.vlt"] = true, ["E116.vlt"] = true, ["hb_a_04.vlt"] = true,
    ["E076.vlt"] = true, ["cts_a_01.vlt"] = true, ["cto_a_02.vlt"] = true,
    ["cr_a_15.vlt"] = true, ["cts_a_02.vlt"] = true, ["es_a_01.vlt"] = true,
    ["E145.vlt"] = true, ["es_a_05.vlt"] = true, ["cto_a_08.vlt"] = true,
    ["cto_a_06.vlt"] = true, ["cto_a_01.vlt"] = true, ["cto_a_05.vlt"] = true,
    ["es_a_03.vlt"] = true, ["cts_a_03.vlt"] = true, ["es_a_04.vlt"] = true,
    ["es_a_02.vlt"] = true, ["cto_a_03.vlt"] = true, ["E186.vlt"] = true,
    ["cto_a_04.vlt"] = true, ["cto_c_01.vlt"] = true, ["E193.vlt"] = true,
    ["E195.vlt"] = true, ["hb_a_03.vlt"] = true, ["es_b_01.vlt"] = true,
    ["hb_a_08.vlt"] = true, ["E215.vlt"] = true, ["E217.vlt"] = true,
    ["E219.vlt"] = true, ["E223.vlt"] = true, ["cts_b_01.vlt"] = true,
    ["sp_a_29.vlt"] = true, ["sp_a_13.vlt"] = true, ["cp_a_22.vlt"] = true,
    ["cto_b_02.vlt"] = true, ["cts_b_04.vlt"] = true, ["cts_b_02.vlt"] = true,
    ["hb_b_05.vlt"] = true, ["cto_b_04.vlt"] = true, ["hb_b_01.vlt"] = true,
    ["E277.vlt"] = true, ["E279.vlt"] = true, ["es_b_03.vlt"] = true,
    ["hb_b_04.vlt"] = true, ["E283.vlt"] = true, ["hb_a_06.vlt"] = true,
    ["hb_a_01.vlt"] = true, ["E286.vlt"] = true, ["es_b_04.vlt"] = true,
    ["E313.vlt"] = true,
}

function on_load()
    nfs = module_base_address('nfs.exe')

    oldGameState = 0
    oldCompleted = 0
    oldRaceName = ""
end

function on_frame()
    raceName = read_ascii(pointer_path(nfs + 0x99B7E4, 0x18, 0x3C), 12)
    gameState = read_int(nfs + 0xF5DF00)
    completed = read_int(pointer_path(nfs + 0x9E5414, 0xB4)) & 0xFF
    loadingEverything = read_int(nfs + 0x9A530C) & 0xFF

    if (oldGameState == 412 and gameState ~= oldGameState)
        or (oldGameState == 56 and loadingEverything == 96) then
        timer_start()
    end

    if loadingEverything ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    local enabled = SPLIT_ALL or ENABLED[raceName]
    local doSplit = false
    if enabled and string.find(raceName, "E", 1, true)
        and gameState ~= oldGameState and gameState == 859 then
        doSplit = true
    elseif enabled and oldCompleted ~= completed
        and (completed == 1 or completed == 2) and gameState ~= 863 then
        doSplit = true
    elseif raceName == "E315.vlt" and (gameState == 32 or gameState == 31)
        and gameState ~= oldGameState then
        doSplit = true
    elseif oldRaceName == "E002.vlt" and raceName == "sp_c_12.vlt" then
        doSplit = true
    end
    if doSplit then
        timer_split()
    end

    oldGameState = gameState
    oldCompleted = completed
    oldRaceName = raceName
end
