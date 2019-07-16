#Draco/drinc/util.g
#Includes/File.g

type ParseState_t = struct
{
    long  ps_nextByteIndex;
    bool  ps_nextParsed;
    ulong ps_nextResult;
};

long IndexByteCount = 60;

proc Get3ByteValue(*ByteArray_t pByteArray;
                   *ParseState_t pParseState;
                   *ulong pCycleCount) *ParseState_t:
    free(pParseState);
    nil /* TODO */
corp;

proc GetNextCycleCount(*ByteArray_t pByteArray;
                       *ParseState_t pParseState;
                       *ulong pCycleCount) *ParseState_t:
    byte bits;
    if pParseState = nil then
        if pByteArray*.ba_byteCount <= IndexByteCount then
            return nil;
        fi;
        pParseState := new(ParseState_t);
        if pParseState = nil then
            return nil;
        fi;
        pParseState*.ps_nextByteIndex := IndexByteCount;
        pParseState*.ps_nextParsed := false;
    fi;
    if pParseState*.ps_nextParsed then
        pCycleCount* := pParseState*.ps_nextResult;
        pParseState*.ps_nextParsed := false;
        return pParseState;
    fi;
    if pParseState*.ps_nextByteIndex >= pByteArray*.ba_byteCount then
        free(pParseState);
        return nil;
    fi;
    bits := (pByteArray*.ba_pBytes + pParseState*.ps_nextByteIndex)*;
    pParseState*.ps_nextByteIndex := pParseState*.ps_nextByteIndex + 1;
    if bits | 0x0F = 0xFF then
        pParseState := Get3ByteValue(pByteArray, pParseState, pCycleCount);
    else
        pCycleCount* := pretend(pByteArray*.ba_pBytes + ((bits >> 2) & 0b111100), *ulong)*;
    fi;
    pParseState*.ps_nextParsed := true;
    bits := bits & 0x0F;
    if bits = 0x0F then
        pParseState := Get3ByteValue(pByteArray,
                                     pParseState,
                                     &pParseState*.ps_nextResult);
    else
        pParseState*.ps_nextResult := pretend(pByteArray*.ba_pBytes + (bits << 2), *ulong)*;
    fi;
    pParseState
corp;