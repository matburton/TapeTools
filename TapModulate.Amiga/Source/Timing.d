#Draco/drinc/util.g

type TimerState_t = struct
{
    ulong ts_cyclesRemaining;
};

proc ReadyTimer() void:
    byte dummy;
    pretend(0xBFEE01, *byte)* := 0b00000000;
    pretend(0xBFED01, *byte)* := 0b01111111;
    pretend(0xBFED01, *byte)* := 0b10000001;
    dummy := pretend(0xBFED01, *byte)*;
corp;

proc SetNextCountdown(ulong nextCount) ulong:
    ulong remainder;
    if nextCount <= 0xFFFF then
        remainder := 0;
    elif nextCount <= 131070 then
        remainder := nextCount >> 1;
        nextCount := nextCount - remainder;
    else
        remainder := nextCount - 0xFFFF;
        nextCount := 0xFFFF;
    fi;
    pretend(0xBFE501, *byte)* := pretend(&nextCount + 2, *byte)*;
    pretend(0xBFE401, *byte)* := pretend(&nextCount + 3, *byte)*;
    remainder
corp;

proc SpinPollForInterrupt() bool:
    if pretend(0xBFED01, *byte)* & 0b10000001 = 0b10000001 then
        return false;
    fi;
    while
        pretend(0xBFED01, *byte)* & 0b10000001 ~= 0b10000001
    do
    od;
    true
corp;

proc WaitForEndOfPreviousCycles(*TimerState_t pTimerState;
                                *ulong pNextCycleCount) *TimerState_t:
    if pTimerState = nil then
        if pNextCycleCount = nil then
            return nil;
        fi;
        pTimerState := new(TimerState_t);
        if pTimerState = nil then
            return nil;
        fi;
        ReadyTimer();
        pTimerState*.ts_cyclesRemaining := SetNextCountdown(pNextCycleCount*);
        pretend(0xBFEE01, *byte)* := 0b00000001;
        return pTimerState;
    fi;
    while
        pTimerState*.ts_cyclesRemaining ~= 0
    do
        pTimerState*.ts_cyclesRemaining := SetNextCountdown(pTimerState*.ts_cyclesRemaining);
        if not SpinPollForInterrupt() then
            free(pTimerState);
            return nil;
        fi;
    od;
    if pNextCycleCount ~= nil then
        pTimerState*.ts_cyclesRemaining := SetNextCountdown(pNextCycleCount*);
    fi;
    if not SpinPollForInterrupt() or pNextCycleCount = nil then
        free(pTimerState);
        return nil;
    fi;
    pTimerState
corp;