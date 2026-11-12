-- Need for Speed: Carbon -> SpeedTool
-- Ported from the LiveSplit ASL by Mr.Mary, TDOG20 and WillTreaty (v1.4).
--
-- NOT PORTED (SpeedTool has no timer-phase/reset access):
--   * Quick Race Mode (interval/lap splits, resets)
--   * gameTime/timing-method handling

local LOAD_VALUES = { [0] = true, [34] = true, [40] = true, [33] = true, [5] = true, [3] = true, [4] = true, [35] = true }

function on_load()
    nfsc = module_base_address('NFSC.exe')

    oldMenuShit = 0
    oldIsFinished = 0
    oldNisCutsceneID = ""
    oldFmv = ""
end

function on_frame()
    menuShit = read_int(nfsc + 0x6980B8)
    isFinished = read_int(nfsc + 0x68A284) & 0xFF
    fmv = read_ascii(nfsc + 0x697A88, 16)
    nisCutsceneID = read_ascii(nfsc + 0x697AD0, 20)
    loading = read_int(pointer_path(nfsc + 0x69970C, 0x0))

    if oldMenuShit == 11 and menuShit == 0 then
        timer_start()
    end

    if LOAD_VALUES[menuShit] or loading ~= 0 then
        timer_set_loading()
    else
        timer_set_not_loading()
    end

    local doSplit = false
    if oldIsFinished == 0 and isFinished ~= 0 then
        doSplit = true
    elseif nisCutsceneID == "ENDINGCAREER22" and oldNisCutsceneID ~= nisCutsceneID then
        doSplit = true
    elseif nisCutsceneID == "ENDINGCAREER23NG" and oldNisCutsceneID ~= nisCutsceneID then
        doSplit = true
    elseif fmv == "Seq06_SamsonFlas" and oldFmv ~= fmv then
        doSplit = true
    elseif fmv == "Seq08_YumiFlashb" and oldFmv ~= fmv then
        doSplit = true
    elseif fmv == "Seq10_ColinFlash" and oldFmv ~= fmv then
        doSplit = true
    elseif fmv == "Seq11_Reversal" and oldFmv ~= fmv then
        doSplit = true
    end
    if doSplit then
        timer_split()
    end

    oldMenuShit = menuShit
    oldIsFinished = isFinished
    oldNisCutsceneID = nisCutsceneID
    oldFmv = fmv
end
