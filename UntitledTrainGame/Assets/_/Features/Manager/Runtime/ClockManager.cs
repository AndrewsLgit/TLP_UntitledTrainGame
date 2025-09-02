using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using SharedData.Runtime;
using SharedData.Runtime.Events;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Manager.Runtime
{
    /// <summary>
    /// Global singleton that manages the in-game clock.
    /// - Holds current time
    /// - Advances/resets loop
    /// - Finds TimeEventGroups in all loaded scenes
    /// - Updates their events when time changes
    /// </summary>
    public class ClockManager : FMono
    {
        #region Variables
        // Start of the Variables region
        
        #region Private
        // Start of the Private region

        private List<TimeEventGroup> _timeEventGroups = new List<TimeEventGroup>();
        
        #endregion

        #region Public
        // Start of the Public region
        
        public static ClockManager Instance { get; private set; }
        
        [Header("Config")]
        public TimeConfig m_TimeConfig;
        
        [Header("Runtime State (Read Only)")]
        public GameTime m_CurrentTime { get; private set; }
        
        public event Action<GameTime> m_OnTimeUpdated;

        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            // enforce singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // initialize time to loop start
            m_CurrentTime = m_TimeConfig.m_LoopStart;

        }

        private void OnEnable()
        {
            // Refresh when scene loaded
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            RefreshEventGroups();
        }

        #endregion
        
        #region Main Methods
        /// <summary>
        /// Advance the clock by a duration (hours + minutes).
        /// </summary>
        public void AdvanceTime(GameTime duration)
        {
            SetTime(m_CurrentTime.AddTime(duration));
            m_OnTimeUpdated?.Invoke(m_CurrentTime);
        } 
        
        /// <summary>
        /// Set the clock directly.
        /// If new newTime is outside the loop range → reset to loop start.
        /// </summary>
        public void SetTime(GameTime newTime)
        {
            int minTime = m_TimeConfig.m_LoopStart.ToTotalMinutes();
            int maxTime = m_TimeConfig.m_LoopEnd.ToTotalMinutes();
            int newTimeToMinues = newTime.ToTotalMinutes();
            
            if (newTimeToMinues < minTime || newTimeToMinues > maxTime)
            {
                // If new time is outside of loop range, reset to loop start
                Warning($"Time {newTime} is outside of loop range [{minTime} - {maxTime}]. Resetting to {m_TimeConfig.m_LoopStart}");
                // m_CurrentTime = m_TimeConfig.m_LoopStart;
                m_CurrentTime = newTime; // to trigger last event's onEnd callback
                //todo: trigger last event's onEnd callback
                CheckAllEvents();
                ResetAllEvents();
                m_CurrentTime = m_TimeConfig.m_LoopStart;
            }
            else m_CurrentTime = newTime;

            CheckAllEvents();
            m_OnTimeUpdated?.Invoke(m_CurrentTime);
        }
        
        #endregion
        
        #region Utils

        /// <summary>
        /// Called when a scene is loaded (additive or single).
        /// Refreshes event groups automatically.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshEventGroups();
        }

        /// <summary>
        /// Find all TimeEventGroups in all loaded scenes.
        /// </summary>
        private void RefreshEventGroups()
        {
            _timeEventGroups = FindObjectsByType<TimeEventGroup>(FindObjectsSortMode.None).ToList();
        }

        /// <summary>
        /// Reset every event in every group (called on loop restart).
        /// </summary>
        private void ResetAllEvents()
        {
            foreach (var group in _timeEventGroups)
                group.ResetEvents();
        }

        /// <summary>
        /// Check all event groups against current time.
        /// </summary>
        private void CheckAllEvents()
        {
            foreach (var group in _timeEventGroups)
                group.CheckEvents(m_CurrentTime);
        }

        /// <summary>
        /// Find the next event with a given tag across all groups.
        /// Used by DebugTool (e.g., jump to next train).
        /// </summary>
        public TimeEvent FindNextEventWithTag(string tag)
        {
            int now = m_CurrentTime.ToTotalMinutes();
            int loopEnd = m_TimeConfig.m_LoopEnd.ToTotalMinutes();
            
            return _timeEventGroups
                .SelectMany(g => g.m_Events)
                .Where(e => (e.m_Tag == tag) && 
                            (e.m_Start.ToTotalMinutes() > now) && 
                            (e.m_Start.ToTotalMinutes() < loopEnd))
                .OrderBy(e => e.m_Start.ToTotalMinutes())
                .FirstOrDefault();
        }
        
        #endregion
    }
}