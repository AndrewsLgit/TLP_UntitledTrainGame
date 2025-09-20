using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using ServiceInterfaces.Runtime;
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
    public class ClockManager : FMono, IClockService
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
        public GameTime CurrentTime { get; private set; }

        [Header("Current Events (Read Only)")] public List<TimeEventGroup> m_CurrentEvents => _timeEventGroups;
        
        public event Action<GameTime> OnTimeUpdated;
        
        public event Action OnLoopEnd;
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
            CurrentTime = m_TimeConfig.m_LoopStart;

        }

        private void OnEnable()
        {
            // Refresh when scene loaded
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            // UnitySceneManager.sceneUnloaded += OnSceneLoaded;
            // UnitySceneManager.activeSceneChanged += OnSceneLoaded;
            // SceneManager.Instance.OnSceneActivated += OnSceneLoaded;
        }

        private void OnDisable()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            // UnitySceneManager.sceneUnloaded -= OnSceneLoaded;
            // UnitySceneManager.activeSceneChanged -= OnSceneLoaded;
            // SceneManager.Instance.OnSceneActivated -= OnSceneLoaded;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            RefreshEventGroups();
            CheckAllEvents();
        }

        #endregion
        
        #region Main Methods
        /// <summary>
        /// Advance the clock by a duration (hours + minutes).
        /// </summary>
        public void AdvanceTime(GameTime duration)
        {
            Info($"Advancing time by {duration}");
            SetTime(CurrentTime.AddTime(duration));
            OnTimeUpdated?.Invoke(CurrentTime);
        }

        /// <summary>
        /// Set the clock directly.
        /// If new newTime is outside the loop range → reset to loop start.
        /// </summary>
        public void SetTime(GameTime newTime)
        {
            int minTime = m_TimeConfig.m_LoopStart.ToTotalMinutes();
            int maxTime = m_TimeConfig.m_LoopEnd.ToTotalMinutes();
            int newTimeToMinutes = newTime.ToTotalMinutes();
            
            if (newTimeToMinutes < minTime || newTimeToMinutes >= maxTime)
            {
                if(newTimeToMinutes >= maxTime)
                    OnLoopEnd?.Invoke();
                
                // If new time is outside of loop range, reset to loop start
                Warning($"Time {newTime} is outside of or at loop range [{GameTime.FromTotalMinutes(minTime)} - {GameTime.FromTotalMinutes(maxTime)}]. Resetting to {m_TimeConfig.m_LoopStart}");
                // CurrentTime = m_TimeConfig.m_LoopStart;
                CurrentTime = newTime; // to trigger last event's onEnd callback
                //todo: trigger last event's onEnd callback
                // CheckAllEvents();
                // ResetAllEvents();
                // CurrentTime = m_TimeConfig.m_LoopStart;
                ResetClock();
            }
            else CurrentTime = newTime;

            RefreshEventGroups();
            CheckAllEvents();
            OnTimeUpdated?.Invoke(CurrentTime);
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
            CheckAllEvents();
            //ResetClock();
        }

        private void OnSceneLoaded(Scene scene)
        {
            RefreshEventGroups();
            CheckAllEvents();
            //ResetClock();       
        }

        private void OnSceneLoaded()
        {
            RefreshEventGroups();
        }

        /// <summary>
        /// Find all TimeEventGroups in all loaded scenes.
        /// </summary>
        private void RefreshEventGroups()
        {
            _timeEventGroups.Clear(); ;
            _timeEventGroups = FindObjectsByType<TimeEventGroup>(FindObjectsSortMode.None).Where(eg => eg.gameObject.scene.name == SceneManager.Instance.CurrentActiveScene).ToList();
            // ResetAllEvents();
            Info($"Found {_timeEventGroups.Count} TimeEventGroups with {_timeEventGroups.ToArray().Length} event groups and {_timeEventGroups.SelectMany(g => g.m_Events).Count()} events in scene {SceneManager.Instance.CurrentActiveScene}.");
        }

        private void ResetClock()
        {
            CheckAllEvents();
            ResetAllEvents();
            RefreshEventGroups();
            SetTime(m_TimeConfig.m_LoopStart);
        }

        /// <summary>
        /// Reset every event in every group (called on loop restart).
        /// </summary>
        private void ResetAllEvents()
        {
            foreach (var group in _timeEventGroups)
                group.ResetEvents();
            
            OnLoopEnd?.Invoke();
        }

        /// <summary>
        /// Check all event groups against current time.
        /// </summary>
        private void CheckAllEvents()
        {
            foreach (var group in _timeEventGroups)
                group.CheckEvents(CurrentTime);
        }

        /// <summary>
        /// Find the next event with a given tag across all groups.
        /// Used by DebugTool (e.g., jump to next train).
        /// </summary>
        public TimeEvent JumpToNextEventWithTag(string tag)
        {
            
            var tagString = tag == String.Empty ? "no tag" : tag;
            InfoInProgress($"Finding next event with tag: {tagString}");
            int now = CurrentTime.ToTotalMinutes();
            int loopEnd = m_TimeConfig.m_LoopEnd.ToTotalMinutes();
            
            // RefreshEventGroups();
            TimeEvent nextEvent = null;

            if (string.Equals(tag, "Sleep", StringComparison.OrdinalIgnoreCase))
            {
                SleepToLoopEnd();
                return null;
            }

            // if (string.IsNullOrEmpty(tag))
            // {
            //     var groupsEvents = _timeEventGroups.SelectMany(g => g.m_Events);
            //     var availableEvents = groupsEvents.Where(e => e.m_Start.ToTotalMinutes() > now && e.m_Start.ToTotalMinutes() < loopEnd);
            //     var orderedEvents = availableEvents.OrderBy(e => e.m_Start.ToTotalMinutes()); 
            //     nextEvent = orderedEvents.FirstOrDefault();
            //     
            //     var nextGroupEvent = _timeEventGroups.SelectMany(g => g.m_Events)
            //         .Where(e => e.m_Start.ToTotalMinutes() > now && e.m_Start.ToTotalMinutes() < loopEnd)
            //         .OrderBy(e => e.m_Start.ToTotalMinutes())
            //         .FirstOrDefault();
            //     
            // }
            if(string.IsNullOrEmpty(tag))
                nextEvent = _timeEventGroups.SelectMany(g => g.m_Events)
                    .Where(e => e.m_Start.ToTotalMinutes() > now && e.m_Start.ToTotalMinutes() < loopEnd)
                    .OrderBy(e => e.m_Start.ToTotalMinutes())
                    .FirstOrDefault();

            else
                nextEvent = _timeEventGroups
                    .SelectMany(g => g.m_Events)
                    .Where(e => (e.m_Tag == tag) &&
                                (e.m_Start.ToTotalMinutes() > now) &&
                                (e.m_Start.ToTotalMinutes() < loopEnd))
                    .OrderBy(e => e.m_Start.ToTotalMinutes())
                    .FirstOrDefault();

            if (nextEvent is null)
            {
                Warning($"No event found with tag {tag}.");
                // m_OnLoopEnd?.Invoke();
                // if(now >= loopEnd)
                //     m_OnLoopEnd?.Invoke();
                // SetTime(m_TimeConfig.m_LoopEnd);
                return nextEvent;
            }
            RouteManager.Instance.RemovePausedRoute();
            // GetNextEvent();
            SetTime(nextEvent.m_Start);
            return nextEvent;
        }

        public TimeEvent GetNextEvent()
        {
            int now = CurrentTime.ToTotalMinutes();
            int loopEnd = m_TimeConfig.m_LoopEnd.ToTotalMinutes();
            
            // RefreshEventGroups();
            
            TimeEvent nextEvent = null;

            nextEvent = _timeEventGroups
                .SelectMany(g => g.m_Events)
                .Where(e => (e.m_Start.ToTotalMinutes() > now) &&
                            (e.m_Start.ToTotalMinutes() < loopEnd))
                .OrderBy(e => e.m_Start.ToTotalMinutes())
                .FirstOrDefault();

            if (nextEvent != null)
            {
                Info($"Found next event: {nextEvent.m_EventName} at {nextEvent.m_Start}.");
                return nextEvent;
            }
            
            Warning($"No event found.");
            return null;

        }

        public void SleepToLoopEnd()
        {
            SetTime(m_TimeConfig.m_LoopEnd);
        }
        //todo: get next train, with arrival time and destination station
        
        #endregion
    }
}