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
    Disable(); /* TODO: Disable DMA? */
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCountA);
        pParseState ~= nil
    do
        cycleCountB := cycleCountA >> 1;
        cycleCountA := cycleCountA - cycleCountB;
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountA);
        if pTimerState = nil then
            cycleEndsMissed := cycleEndsMissed + 1;
        fi;
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountB);
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

/*
proc SpeedTest() void:
    ByteArray_t fileBytes;
    *ParseState_t pParseState;
    *TimerState_t pTimerState;
    ulong cycleCountA, cycleCountB;
    pParseState := nil;  
    pTimerState := nil;
    cycleCountB := 2000;
    if not LoadFile("out.tap.amiga", &fileBytes) then
        writeln("Failed to load 'out.tap.amiga'");
        return;
    fi;
    Disable();
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCountA);
        pParseState ~= nil
    do
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCountB);
        if pTimerState = nil then
            Enable();
            FreeByteArray(&fileBytes);
            writeln("With parsing cycle minimum ", cycleCountB);
            while
                pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCountA);
                pParseState ~= nil
            do
            od;
            return;
        fi;
        cycleCountB := cycleCountB - 1;;
    od;
    ignore(WaitForEndOfPreviousCycles(pTimerState, nil));
    Enable();
    FreeByteArray(&fileBytes);
    writeln("File too short");
corp;
*/

/* http://coppershade.org/asmskool/SOURCES/Developing-Demo-Effects/DDE1/Coppershade-DDE1/PhotonsMiniWrapper1.04!.S */

proc SpeedTest() void:
    *TimerState_t pTimerState;
    ulong cycleCount;
    pTimerState := nil;
    cycleCount := 200;
    Disable();
    pretend(0xDFF096, *uint)* := 0b0000011111111111; /* Disable DMA */
    while
        pTimerState := WaitForEndOfPreviousCycles(pTimerState, &cycleCount);
        pTimerState ~= nil
    do
        cycleCount := cycleCount - 1;
    od;
    pretend(0xDFF096, *uint)* := 0b1000001111111111; /* Enable DMA */
    Enable();
    writeln("No parsing cycle minimum ", cycleCount);
corp;