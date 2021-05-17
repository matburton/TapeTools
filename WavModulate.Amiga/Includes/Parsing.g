
type ParseState_t = struct {};

extern GetStartLow(*ByteArray_t pByteArray) bool;

/* cycleCount is in Amiga CIA colour clock cycles */
/* Returns nil once there are no more cycles counts */
/* pParseState should be nil when first called */
/* Returns nil once there are no more cycles counts */
/* cycleCount is not set when nil is returned */
/* This must be called until it returns nil */
extern GetNextCycleCount(*ByteArray_t pByteArray;
                         *ParseState_t pParseState;
                         *ulong pCycleCount) *ParseState_t;