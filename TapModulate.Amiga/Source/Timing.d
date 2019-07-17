#Draco/drinc/util.g

type TimerState_t = struct
{
    ulong ts_cyclesRemaining;
};

proc WaitForEndOfPreviousCycles(*TimerState_t pTimerState;
                                *ulong pNextCycleCount) *TimerState_t:
    nil
corp;