#Draco/drinc/exec/tasks.g
#Includes/File.g
#Includes/Parsing.g
#Includes/Timing.g

proc Start() void:
    ByteArray_t fileBytes;
    bool low;
    *ParseState_t pParseState;
    *TimerState_t pTimerState;
    ulong cycleCount, cycleEndsMissed;
    pParseState := nil;  
    pTimerState := nil;
    cycleEndsMissed := 0;
    if not LoadFile("out.wav.amiga", &fileBytes) then
        writeln("Failed to load 'out.wav.amiga'");
        return;
    fi;
    low := GetStartLow(&fileBytes);
    Disable();
    pretend(0xBFE301, *byte)* := 0xFF;
    /* I assume this is the right way around but haven't checked */
    if low = true then
        pretend(0xBFE101, *byte)* := 0;
    else
        pretend(0xBFE101, *byte)* := 0b10;
    fi;
    cycleCount := 709379 * 5;
    pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCount);
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCount);
        pParseState ~= nil
    do
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCount);
        /* I assume this is the right way around but haven't checked */
        if low = true then
            pretend(0xBFE101, *byte)* := 0;
        else
            pretend(0xBFE101, *byte)* := 0b10;
        fi;
        low := not low;
        if pTimerState = nil then
            cycleEndsMissed := cycleEndsMissed + 1;
        fi;
    od;
    ignore(WaitForEndOfPreviousCycles(pTimerState, nil));
    Enable();
    FreeByteArray(&fileBytes);
    if cycleEndsMissed > 0 then
        writeln("WARNING: Missed cycle ends ", cycleEndsMissed, " times");
    fi;
corp;
