#Draco/drinc/exec/tasks.g
#Includes/File.g
#Includes/Parsing.g
#Includes/Timing.g

proc Start() void:
    ByteArray_t fileBytes;
    *ParseState_t pParseState;
    *TimerState_t pTimerState;
    ulong cycleCount, cycleEndsMissed;
    pParseState := nil;  
    pTimerState := nil;
    cycleEndsMissed := 0;
    if not LoadFile("out.tap.amiga", &fileBytes) then
        writeln("Failed to load 'out.tap.amiga'");
        return;
    fi;
    Disable();
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCount);
        pParseState ~= nil
    do
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCount);
        if pTimerState = nil then
            cycleEndsMissed := cycleEndsMissed + 1;
        fi;
    od;
    ignore(WaitForEndOfPreviousCycles(pTimerState, nil));
    Enable();
    writeln("WARNING: Missed cycle ends ", cycleEndsMissed, " times");
    FreeByteArray(&fileBytes);
corp;