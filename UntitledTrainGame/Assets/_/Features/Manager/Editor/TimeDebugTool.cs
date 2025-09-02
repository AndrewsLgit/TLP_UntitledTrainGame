using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Manager.Editor
{
    /// <summary>
    /// Debug-only tool for designers to skip time without replaying the loop.
    /// - Can set time manually (hours/minutes)
    /// - Can jump to next event with a given tag (e.g. Train)
    /// </summary>
    public class TimeDebugTool : FMono
    {
        [Header("Target Time (HH:MM)")]
        [Range(0,23)] public int m_Hours;
        [Range(0,59)] public int m_Minutes;
        
        private ClockManager _clockManager;

        private void Start()
        {
            _clockManager = ClockManager.Instance;
        }

        
        private void OnGUI()
        {
            
#if UNITY_EDITOR
            if(ClockManager.Instance == null) return;
            if(_clockManager == null) return;
            
            GUILayout.BeginVertical("box");
            GUILayout.Label("Time Debug Tool");
            
            GUILayout.Label($"Current Time: {_clockManager.m_CurrentTime.ToString()}");
            GUILayout.Space(10);
            
            GUILayout.Label($"Target Time: {m_Hours:D2}:{m_Minutes:D2}");

            if (GUILayout.Button("Set Time"))
                _clockManager.SetTime(new GameTime(m_Hours, m_Minutes));
            
            GUILayout.Space(10);
            GUILayout.Label("Quick Jumps:");

            if (GUILayout.Button("Jump to Next Train"))
            {
                var timeEvent = _clockManager.FindNextEventWithTag("Train");
                
                if (timeEvent != null)
                    _clockManager.SetTime(timeEvent.m_Start);
                else
                    Warning("No train train event found on this loop.");
            }
            
            GUILayout.EndVertical();
#endif
        }
        
    }
}