#Draco/drinc/exec/tasks.g
#Includes/File.g
#Includes/Parsing.g
#Includes/Timing.g

proc Start() void:
    ByteArray_t fileBytes;
    *ParseState_t pParseState;
    *TimerState_t pTimerState;
    ulong cycleCountA, cycleCountB, cycleEndsMissed;
    pParseState := nil;  
    pTimerState := nil;
    cycleEndsMissed := 0;
    if not LoadFile("out.tap.amiga", &fileBytes) then
        writeln("Failed to load 'out.tap.amiga'");
        return;
    fi;
    Disable();
    pretend(0xBFE301, *byte)* := 0xFF;
    pretend(0xBFE101, *byte)* := 0;
    cycleCountA := 709379 * 6;
    pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountA);
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCountA);
        pParseState ~= nil
    do
        cycleCountB := cycleCountA >> 1;
        cycleCountA := cycleCountA - cycleCountB;
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountA);
        pretend(0xBFE101, *byte)* := 0b10;
        if pTimerState = nil then
            cycleEndsMissed := cycleEndsMissed + 1;
        fi;
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountB);
        pretend(0xBFE101, *byte)* := 0;
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