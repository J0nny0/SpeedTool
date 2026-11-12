-- Forza Motorsport 6: Apex (UWP/PC) -> SpeedTool
-- Ported from the FM6 Apex Loadless timer ASL by J0nny0
-- (speedrun.com/fm6 resource).
--
-- Load remover only - the source ASL has no start/split logic.
--
-- Notes:
--   * UWP game: SpeedTool must run elevated/with the right permissions to
--     read the process, same as LiveSplit needed for this title.

function on_load()
    game = module_base_address('forzamotorsportapex.exe')

    wasLoading = false
end

function on_frame()
    load = read_int(pointer_path(game + 0x51D0F58, 0x10, 0x8, 0x10, 0xD0, 0x218, 0x1D8)) & 0xFF

    if load == 1 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end
    wasLoading = load == 1
end
