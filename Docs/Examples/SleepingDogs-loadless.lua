-- Sleeping Dogs -> SpeedTool
-- Ported from the LiveSplit load-remover ASL.
--
-- Two game versions, selected with VERSION below:
--   "HKShip"    = original Sleeping Dogs (2012)
--   "SDHDShip"  = Sleeping Dogs: Definitive Edition (2014)
--
-- Load remover only - the source ASL has no start/split logic.

local VERSION = "SDHDShip"

local OFF = {
    HKShip   = { loading0 = 0x000620E0, loadingOff = 0x418 },
    SDHDShip = { loading0 = 0x0207B000, loadingOff = 0x260 },
}

function on_load()
    game = module_base_address(VERSION == "HKShip" and 'HKShip.exe' or 'SDHDShip.exe')
    o = OFF[VERSION]
end

function on_frame()
    loading = read_int(pointer_path(game + o.loading0, o.loadingOff))

    if loading == 1 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end
end
