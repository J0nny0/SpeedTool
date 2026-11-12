-- Need for Speed: Hot Pursuit Remastered -> SpeedTool
-- Ported from the LiveSplit ASL by SlivenKage.
--
-- Notes:
--   * The ASL's start condition is level-triggered (autoStart == 8); here the
--     timer starts on the rising edge of that value, which behaves the same
--     for a single start per run.
--   * exit block (IsGameTimePaused) dropped - no equivalent in SpeedTool.

function on_load()
    game = module_base_address('NFS11Remastered.exe')

    oldAutoStart = 0
    oldSplitFlag = 0
end

function on_frame()
    loadingFlag = read_int(pointer_path(game + 0x12B62B0, 0x0, 0x30, 0x40, 0x138, 0x8, 0x18, 0x460))
    loadingFlag2 = read_int(game + 0x1297148 + 0x424)
    loadingFlag3 = read_int(game + 0x1297068 + 0xF34)
    autoStart = read_int(pointer_path(game + 0x12B62B0, 0x0, 0x30, 0x40, 0x138, 0x8, 0x18, 0x460))
    splitFlag = read_int(pointer_path(game + 0x12B62B0, 0x0, 0x30, 0x60, 0x40, 0x98, 0x60, 0x58))

    if autoStart == 8 and oldAutoStart ~= 8 then
        timer_start()
    end

    if loadingFlag == 8 or loadingFlag2 == 1106247680 or loadingFlag3 == 1065353216 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    if splitFlag == 0 and oldSplitFlag ~= 0 then
        timer_split()
    end

    oldAutoStart = autoStart
    oldSplitFlag = splitFlag
end
