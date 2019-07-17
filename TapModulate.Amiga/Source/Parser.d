#Draco/drinc/util.g
#Includes/File.g

type ParseState_t = struct
{
    *byte ps_pNextByte;
    bool  ps_nextParsed;
    ulong ps_nextResult;
};

long IndexByteCount = 60;

proc Get3ByteValue(*ByteArray_t pByteArray;
                   *ParseState_t pParseState;
                   *ulong pCycleCount) bool:
    if pParseState*.ps_pNextByte + 2 > pByteArray*.ba_pLastByte then
        pParseState*.ps_pNextByte := pByteArray*.ba_pLastByte + 1;
        return false;
    fi;
    if pretend(pParseState*.ps_pNextByte, ulong) % 2 = 0 then
        pCycleCount* := pretend(pParseState*.ps_pNextByte, *ulong)* >> 8;
    else
        pCycleCount* := pretend(pParseState*.ps_pNextByte - 1, *ulong)* & 0x00FFFFFF;
    fi;
    pParseState*.ps_pNextByte := pParseState*.ps_pNextByte + 3;
    true
corp;

proc GetNextCycleCount(*ByteArray_t pByteArray;
                       *ParseState_t pParseState;
                       *ulong pCycleCount) *ParseState_t:
    byte bits;
    if pParseState = nil then
        if pByteArray*.ba_pLastByte - pByteArray*.ba_pFirstByte < IndexByteCount then
            return nil;
        fi;
        pParseState := new(ParseState_t);
        if pParseState = nil then
            return nil;
        fi;
        pParseState*.ps_pNextByte := pByteArray*.ba_pFirstByte + IndexByteCount;
        pParseState*.ps_nextParsed := false;
    fi;
    if pParseState*.ps_nextParsed then
        pCycleCount* := pParseState*.ps_nextResult;
        pParseState*.ps_nextParsed := false;
        return pParseState;
    fi;
    if pParseState*.ps_pNextByte > pByteArray*.ba_pLastByte then
        free(pParseState);
        return nil;
    fi;
    bits := pParseState*.ps_pNextByte*;
    pParseState*.ps_pNextByte := pParseState*.ps_pNextByte + 1;
    pParseState*.ps_nextParsed := true;
    if bits | 0x0F = 0xFF then
        if not Get3ByteValue(pByteArray, pParseState, pCycleCount) then
            free(pParseState);
            return nil;
        fi;
    else
        pCycleCount* := pretend(pByteArray*.ba_pFirstByte + ((bits >> 2) & 0b111100), *ulong)*;
    fi;
    bits := bits & 0x0F;
    if bits = 0x0F then
        if not Get3ByteValue(pByteArray, pParseState, &pParseState*.ps_nextResult) then
            pParseState*.ps_nextParsed := false;
        fi;
    else
        pParseState*.ps_nextResult := pretend(pByteArray*.ba_pFirstByte + (bits << 2), *ulong)*;
    fi;
    pParseState
corp;