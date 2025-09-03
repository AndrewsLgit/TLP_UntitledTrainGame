using Foundation.Runtime;
using SharedData.Runtime.Events;
using UnityEngine;

namespace SharedData.Runtime
{
    /// <summary>
    /// Each scene can have one or more TimeEventGroup components.
    /// They hold scene-local TimeEvents.
    /// ClockManager will find and update all groups automatically.
    /// </summary>
    public class TimeEventGroup : FMono
    {
        [Header("Scene Events")]
        public TimeEvent[] m_Events;
        
        /// <summary>
        /// Reset all events in this group to inactive.
        /// Called when the loop restarts.
        /// </summary>
        public void ResetEvents()
        {
            foreach (var e in m_Events)
                e.m_IsActive = false;
        }

        /// <summary>
        /// Check which events should start or end given the current time.
        /// Called by ClockManager whenever time advances.
        /// </summary>
        public void CheckEvents(GameTime currentTime)
        {
            int now = currentTime.ToTotalMinutes();

            foreach (var timeEvent in m_Events)
            {
                int start = timeEvent.m_Start.ToTotalMinutes();
                int end = timeEvent.m_End.ToTotalMinutes();
                
                // event should become active
                if (!timeEvent.m_IsActive && now >= start && now < end)
                {
                    timeEvent.m_IsActive = true;
                    timeEvent.m_OnStart?.Invoke();
                }
                // event should become inactive
                else if (timeEvent.m_IsActive && now >= end)
                {
                    timeEvent.m_IsActive = false;
                    timeEvent.m_OnEnd?.Invoke();
                }
            }
        }
        
    }
}
