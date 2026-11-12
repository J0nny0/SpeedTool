-- Crashday (original 1.1/1.2) -> SpeedTool
-- Ported from the LiveSplit ASL by St1ngLeR (speedrun.com/crashday resource).
-- NOT compatible with Crashday: Redline Edition (different exe).
--
-- Notes:
--   * Start is level-triggered, exactly like the ASL: the timer starts
--     whenever loading_state == 0 and career_event_num == 3 and the timer is
--     not running (timer_start is a no-op while running).
--   * The ASL's exit block (IsGameTimePaused = true) has no SpeedTool
--     equivalent and is dropped; this ASL has no gameTime handling anyway.

function on_load()
    game = module_base_address('Crashday.exe')

    oldRaceState = 0
end

function on_frame()
    isCareer = read_int(game + 0x3A87F8) & 0xFF
    careerEventNum = read_int(game + 0x38E778) & 0xFF
    loadingState = read_int(game + 0x30EB5C) & 0xFF
    raceState = read_int(pointer_path(game + 0x38D5F0, 0x80)) & 0xFF

    if loadingState == 0 and careerEventNum == 3 then
        timer_start()
    end

    if loadingState == 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    if raceState == 4 and oldRaceState ~= 4 and isCareer == 1 then
        timer_split()
    end

    oldRaceState = raceState
end
