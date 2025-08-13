using System.Collections.Generic;
using Foundation.Runtime;
using UnityEngine;

namespace Tools.Runtime
{
    public class SceneLimitsManager : FMono
    {
        #region Private Variables
        
        private List<SceneLimitMarker> _sceneLimits = new List<SceneLimitMarker>();
        
        #endregion
        
        private void Awake()
        {
            // find all markers in scene
            _sceneLimits.AddRange(FindObjectsByType<SceneLimitMarker>(FindObjectsSortMode.None));
        }

        public Vector3 GetClosestLimitPoint(Vector3 target)
        {
            Vector3 closestPoint = target;
            float closestDistance = float.MaxValue;

            foreach (var sceneLimitMarker in _sceneLimits)
            {
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
        
    }
}