using System;
using SharedData.Runtime;
using SharedData.Runtime.Events;

namespace ServiceInterfaces.Runtime
{
    // Time/Clock service
    public interface IClockService
    {
        GameTime CurrentTime { get; }
        event Action<GameTime> OnTimeUpdated;
        event Action OnLoopEnd;

        // Mutations
        void SetTime(GameTime newTime);
        void AdvanceTime(GameTime duration);

        // Query or jump helper; you can later split into QueryNextEvent/AdvanceToNextEvent if you want pure queries
        TimeEvent JumpToNextEventWithTag(string tag);
        TimeEvent GetNextEvent();
        void SleepToLoopEnd();
    }

}