-- Need for Speed: Payback -> SpeedTool
-- Ported from the LiveSplit ASL (Standard Origin Version).
--
-- WARNING: These offsets were scanned for the Origin release. The Steam
-- release may need different offsets - verify the loading flag with a memory
-- viewer before trusting the script.
--
-- Notes:
--   * No split logic exists in the ASL (loadless timing only).
--   * gameTime/timing-method handling dropped (no equivalent in SpeedTool).

function on_load()
    game = module_base_address('NeedForSpeedPayback.exe')

    oldLoading = 0
end

function on_frame()
    loading = read_int(pointer_path(game + 0x37DF618, 0x80, 0xB0, 0x10, 0x28, 0x68, 0x188, 0x128)) & 0xFF

    if oldLoading == 0 and loading ~= 0 then
        timer_start()
    end

    if loading ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    oldLoading = loading
end
