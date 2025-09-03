using System;
using UnityEngine;
using UnityEngine.Events;

namespace SharedData.Runtime.Events
{
    /// <summary>
    /// Represents a single time-ranged event inside a scene.
    /// Active during [start, end).
    /// Example: Train spawns at 11:00 and despawns at 11:30.
    /// </summary>
    [Serializable]
    public class TimeEvent
    {
        public string m_EventName;
        
        [Header("Duration")]
        public GameTime m_Start; // When event becomes active
        public GameTime m_End; // When event ends
        
        [Header("Optional Tag")]
        [Tooltip("Useful for quick jumps in the DebugTool (e.g. 'Train').")]
        public string m_Tag = "";

        [Header("Actions")] 
        public UnityEvent m_OnStart;
        public UnityEvent m_OnEnd;
        
        [HideInInspector]
        public bool m_IsActive; // runtime flag
    }
}