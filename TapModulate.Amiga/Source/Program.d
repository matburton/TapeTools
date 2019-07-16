#Draco/drinc/exec/tasks.g
#Draco/drinc/util.g
#Includes/File.g
#Includes/Parser.g

proc Start() void:
    ByteArray_t fileBytes;
    *ParseState_t pParseState;
    ulong cycleCount;
    MerrorSet(true);
    if not LoadFile("out.tap.amiga", &fileBytes) then
        writeln("Failed to load 'out.tap.amiga'");
        return;
    fi;
    /* Disable(); */
    pParseState := nil;  
    while
        pParseState := GetNextCycleCount(&fileBytes, pParseState, &cycleCount);
        pParseState ~= nil
    do
        /* writeln("Cycles ", cycleCount); */
    od;
    /* Enable(); */
    Mfree(fileBytes.ba_pBytes, fileBytes.ba_byteCount);
corp;