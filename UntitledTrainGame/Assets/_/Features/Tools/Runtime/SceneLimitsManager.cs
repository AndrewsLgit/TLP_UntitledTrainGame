using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tools.Runtime
{
    public class SceneLimitsManager : FMono
    {
        #region Private Variables
        
        private List<SceneLimitMarker> _sceneLimits = new List<SceneLimitMarker>();
        
        #endregion
        
        #region Public Variables
        
        public static SceneLimitsManager Instance { get; private set; }
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            // find all markers in scene
            _sceneLimits.AddRange(FindObjectsByType<SceneLimitMarker>(FindObjectsSortMode.None));
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        #endregion

        #region Main Methods
        
        public Vector3 GetClosestLimitPoint(Vector3 target)
        {
            if (_sceneLimits.Count == 0 || _sceneLimits.Any(x => x == null))
            {
                _sceneLimits.Clear();
                var markers = FindObjectsByType<SceneLimitMarker>(FindObjectsSortMode.None);
                _sceneLimits.AddRange(markers.Where(x => x != null));
                
                if (_sceneLimits.Count == 0)
                    return target;
            }
            
            Vector3 closestPoint = target;
            float closestDistance = float.MaxValue;

            foreach (var sceneLimitMarker in _sceneLimits)
            {
                if (sceneLimitMarker == null) continue;
                
                var collider = sceneLimitMarker.GetComponent<Collider>();
                if (collider == null) continue;
                
                Vector3 point = collider.ClosestPoint(target);
                float distance = Vector3.Distance(target, point);
                // float distance = Vector3.Distance(point, target);
                if (!(distance < closestDistance)) continue;
                
                closestDistance = distance;
                closestPoint = point;
            }
            InfoDone($"Found closest point: {closestDistance}");
            return closestPoint;
        }
        
        #endregion
        
        #region Utils
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // _sceneLimits.Clear();
            // _sceneLimits.AddRange(FindObjectsByType<SceneLimitMarker>(FindObjectsSortMode.None));

            StartCoroutine(RefreshMarkersAfterDelay());
        }

        private IEnumerator RefreshMarkersAfterDelay()
        {
            yield return null;
            try
            {
                _sceneLimits.Clear();
                var markers = FindObjectsByType<SceneLimitMarker>(FindObjectsSortMode.None);
                _sceneLimits.AddRange(markers.Where(marker => marker != null));
            }
            catch (Exception e)
            {
                Error($"Marker refresh failed: {e.Message}");
                // throw;
            }
        }
        
        #endregion
        
    }
}