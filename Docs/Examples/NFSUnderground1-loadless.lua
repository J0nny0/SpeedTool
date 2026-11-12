-- Need for Speed: Underground (2003) -> SpeedTool
-- Ported from the LiveSplit ASL by Jujstme (EU/US versions).
--
-- The ASL detects EU vs US by module memory size, which SpeedTool cannot do.
-- Set VERSION to "EU" or "US" below to match your game.
--
-- Notes:
--   * No reset logic existed in the ASL (none is implemented here).
--   * isLoading is handled with timer_set_loading()/timer_set_not_loading().

local VERSION = "EU"

local OFF = {
    EU = {
        fmvActive      = 0x00333F74,
        fmvName        = 0x0038EF70,
        trackId        = 0x0038A2E0,
        trackLaps      = 0x0038A30C,
        trackProgress0 = 0x00336194,
        loading0       = 0x00335DF4,
    },
    US = {
        fmvActive      = 0x00333F74,
        fmvName        = 0x0038EF80,
        trackId        = 0x0038A2F0,
        trackLaps      = 0x0038A31C,
        trackProgress0 = 0x0033619C,
        loading0       = 0x00335DFC,
    },
}

function on_load()
    base = module_base_address('speed.exe')
    o = OFF[VERSION]

    oldFmvActive = 0
    oldTrackProgress = 0.0
end

function on_frame()
    fmvActive = read_int(base + o.fmvActive) & 0xFF
    fmvName = read_ascii(base + o.fmvName, 8)
    trackId = read_int(base + o.trackId)
    trackLaps = read_int(base + o.trackLaps) & 0xFF
    trackProgress = read_float(pointer_path(base + o.trackProgress0, 0x44))
    loading = read_int(pointer_path(base + o.loading0, 0x0)) & 0xFF

    if fmvName == "01_DAY" and fmvActive ~= 0 and oldFmvActive == 0 then
        timer_start()
    end

    if loading ~= 244 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    if loading == 244 and trackId ~= 11 then
        local lapRace = (trackId >= 1001 and trackId <= 1008)
            or (trackId >= 1301 and trackId <= 1308)
        local doSplit = false
        if lapRace then
            doSplit = trackProgress > oldTrackProgress and trackProgress >= trackLaps
        else
            doSplit = trackProgress > oldTrackProgress and oldTrackProgress > 0.95 and trackProgress >= 1
        end
        if doSplit then
            timer_split()
        end
    end

    oldFmvActive = fmvActive
    oldTrackProgress = trackProgress
end
