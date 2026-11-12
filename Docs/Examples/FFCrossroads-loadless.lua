-- Fast & Furious: Crossroads -> SpeedTool
-- Ported from the LiveSplit ASL (load remover only, no start/split logic).
--
-- Notes:
--   * The ASL author noted these addresses are based on guesses (their
--     debugger crashed the game) - verify before trusting.
--   * 64-bit game; FMV flag lives in bink2w64.dll.

function on_load()
    game = module_base_address('ffcrossroads.exe')
    bink = module_base_address('bink2w64.dll')

    wasLoading = false
end

function on_frame()
    loadingScreen = read_int(game + 0x167C8CC) & 0xFF
    fmvPlaying = read_int(bink + 0x57B2C) & 0xFF
    levelReloadScreen = read_int(game + 0x19E2D88) & 0xFF

    local loading = loadingScreen ~= 0 or fmvPlaying ~= 0 or levelReloadScreen ~= 0
    if loading then
        timer_set_loading()
    else
        timer_set_not_loading()
    end
    wasLoading = loading
end
