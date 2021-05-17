
type TimerState_t = struct {};

/* Returns nil if this was called too late */
/* pNextCycleCount should be nil if there is no next period */
/* To abort timing this must be called with pNextCycleCount nil */
/* When pNextCycleCount this returns nil */
extern WaitForEndOfPreviousCycles(*TimerState_t pTimerState;
                                  *ulong pNextCycleCount) *TimerState_t;