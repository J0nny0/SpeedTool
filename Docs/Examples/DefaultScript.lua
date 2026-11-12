-- Default script template (the same text the game editor starts with).
-- Copy this as a starting point for a new autosplitter script.
--
-- on_load()  is executed once when the script is loaded
-- on_frame() is executed on each frame (about 250 times per second on Linux)

function on_load()
    -- this function will be executed when the script is loaded
end

function on_frame()
    -- this function will be executed on each frame

    --[[
        Try hovering over those highlighted functions to see what they do :)
        read_int(0xDEADBEEF)
        read_float(0xDEADBEEF)
        read_long(0xDEADBEEF)
        read_double(0xDEADBEEF)
        read_bytes(0xDEADBEEF, 16)
        read_ascii(0xDEADBEEF, 16)
        pointer_path(0xDEADBEEF, 0xC, 0x10, 0xCC)
        module_base_address('kernel32.dll')

        timer_set_loading()
        timer_set_not_loading()

        timer_start()
        timer_split()

        debug_message('Hello, world!')
        debug_message(string.format('%d %d', 10, 20))
    ]]
end
