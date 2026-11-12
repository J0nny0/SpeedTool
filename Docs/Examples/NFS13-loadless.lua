-- Loadless timing for Need for Speed: Most Wanted (2012) -- NFS13.exe
-- Converted from the LiveSplit .asl definition of the same game.
-- Works with the Linux external autosplitter (Proton/Wine) and the
-- Windows injected autosplitter alike.
--
-- Start condition: initialLoad transitions 1 -> 0 (leaving the very first
-- loading screen). Loading state: any of the three loaders active.

function on_load()
    nfs13 = module_base_address('NFS13.exe')
    oldInitialLoad = 0
end

function on_frame()
    -- state("NFS13") memory definitions
    initialLoad = read_int(pointer_path(nfs13 + 0xCF852C, 0x8C, 0x58, 0x10, 0xC, 0x3B0, 0xA4, 0x3C0))
    ingameLoading = read_int(pointer_path(nfs13 + 0xCF852C, 0xFD0))
    changingCarsLoad = read_int(pointer_path(nfs13 + 0xCF8580, 0x0, 0x8, 0xBC4))

    -- start: old.initialLoad == 1 && current.initialLoad == 0
    if (oldInitialLoad == 1 and initialLoad == 0) then
        timer_start()
    end

    -- isLoading
    if (ingameLoading == 1 or changingCarsLoad == 4 or initialLoad == 1) then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    oldInitialLoad = initialLoad
end
