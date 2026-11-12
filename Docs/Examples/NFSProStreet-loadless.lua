-- Need for Speed: ProStreet -> SpeedTool
-- Ported from the LiveSplit ASL (v1.1).
--
-- Notes:
--   * The ASL's split logic counts loading screens (update block) and splits
--     on the second loading screen after the career intro FMV. The counter is
--     kept in a global here, reset when the timer starts.
--   * exit block (IsGameTimePaused) dropped - no equivalent in SpeedTool.

function on_load()
    nfs = module_base_address('nfs.exe')

    oldLoading = 0
    oldFmv = ""
    oldRacedayPoints = 0
    splitCount = 0
end

function on_frame()
    loading = read_int(nfs + 0x7FD3A4)
    racedayPoints = read_int(pointer_path(nfs + 0x65A0B0, 0x48, 0x8, 0x14, 0x34, 0x320))
    racedayID = read_int(pointer_path(nfs + 0x6B2964, 0xB8, 0x4, 0x4, 0x0, 0xC)) & 0xFF
    fmv = read_ascii(nfs + 0x6B27E0, 12)

    local loadingEdge = oldLoading == 0 and loading ~= 0
    if loadingEdge then
        splitCount = splitCount + 1
    end

    if oldFmv ~= fmv and fmv == "fmv01_career" then
        splitCount = 0
        timer_start()
    end

    if splitCount == 2 and loadingEdge then
        timer_split()
    elseif racedayID == 202 and oldRacedayPoints < 19200 and racedayPoints >= 19200 then
        timer_split()
    end

    if loading ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    oldLoading = loading
    oldFmv = fmv
    oldRacedayPoints = racedayPoints
end
