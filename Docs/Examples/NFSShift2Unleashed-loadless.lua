-- Shift 2: Unleashed -> SpeedTool
-- Ported from the LiveSplit ASL by Balathruin and WillTreaty.
--
-- The ASL detects "Small Exe" vs "Big Exe" by module memory size, which
-- SpeedTool cannot do. Set VERSION to "Small Exe" or "Big Exe" below to match
-- your game exe (test both if unsure).
--
-- gameTime/timing-method handling dropped (no equivalent in SpeedTool).

local VERSION = "Small Exe"

local FINISH_SPLIT = true

local OFF = {
    ["Small Exe"] = {
        loading0 = 0x17714,
        movie0   = 0xAB70A0,
        finish0  = 0x60D7DC,
        finishPath = { 0x450 },
    },
    ["Big Exe"] = {
        loading0 = 0x17654,
        movie0   = 0xAB40A0,
        finish0  = 0x4787AC,
        finishPath = { 0x3A0 },
    },
}

function on_load()
    shift2u = module_base_address('shift2u.exe')
    o = OFF[VERSION]

    oldMovie = 0
    oldFinish = 0
end

function on_frame()
    loading = read_int(pointer_path(shift2u + o.loading0, 0x400))
    movie = read_int(pointer_path(shift2u + o.movie0, 0x19C)) & 0xFF
    finish = read_int(pointer_path(shift2u + o.finish0, table.unpack(o.finishPath)))

    if movie ~= 0 and oldMovie == 0 then
        timer_start()
    end

    if loading ~= 0 and movie == 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    if FINISH_SPLIT and finish == 3 and oldFinish == 2 then
        timer_split()
    end

    oldMovie = movie
    oldFinish = finish
end
