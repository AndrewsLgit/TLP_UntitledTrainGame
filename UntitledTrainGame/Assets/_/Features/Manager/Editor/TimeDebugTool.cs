using Manager.Runtime;
using SharedData.Runtime;
using UnityEditor;
using UnityEngine;

namespace Manager.Editor
{
    /// <summary>
    /// Debug-only tool for designers to skip time without replaying the loop.
    /// - Can set time manually (hours/minutes)
    /// - Can jump to next event with a given tag (e.g. Train)
    /// </summary>
    public class TimeDebugTool : EditorWindow
    {
        private int _hours;
        private int _minutes;
        private string _tag = "Train";
        
        private ClockManager _clockManager;

        [MenuItem("Custom Tool/Time Debug Tool")]
        public static void ShowWindow()
        {
            GetWindow<TimeDebugTool>("Time Debug Tool");
        }
        private void OnGUI()
        {
            
// #if UNITY_EDITOR
            if(ClockManager.Instance == null) return;
            _clockManager = ClockManager.Instance;
            if(_clockManager == null) return;
            
            // GUILayout.BeginVertical("box");
            // GUILayout.Label("Time Debug Tool");
            GUILayout.Label("Manual Time Control", EditorStyles.boldLabel);
            
            GUILayout.Label($"Current Time: {_clockManager.CurrentTime.ToString()}");
            GUILayout.Space(10);
            
            // GUILayout.Label($"Target Time: {m_Hours:D2}:{m_Minutes:D2}");

            _hours = EditorGUILayout.IntSlider("Hours", _hours, 0, 23);
            _minutes = EditorGUILayout.IntSlider("Minutes", _minutes, 0, 59);
            
            if (GUILayout.Button("Set Time"))
                _clockManager.SetTime(new GameTime(_hours, _minutes));
            
            GUILayout.Space(15);
            GUILayout.Label("Quick Jumps", EditorStyles.boldLabel);;

            _tag = EditorGUILayout.TextField("Tag", _tag);

            if (GUILayout.Button("Jump to Next Event"))
            {
                Debug.Log($"Using tag: {_tag}");
                var timeEvent = _clockManager.JumpToNextEventWithTag(_tag);

                if (timeEvent != null)
                {
                    // _clockManager.SetTime(timeEvent.m_Start);
                    Debug.Log($"Jumped to next event at {timeEvent.m_Start}");
                }
                else
                    Debug.LogWarning("No train train event found on this loop.");
            }
            
            GUILayout.EndVertical();
// #endif
        }
        
    }
}