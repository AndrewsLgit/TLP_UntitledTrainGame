using Manager.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class PendingTrainSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _pendingTrainPrefab;
        private RouteManager _routeManager;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _routeManager = RouteManager.Instance;
            _pendingTrainPrefab.SetActive(false);
            Assert.IsNotNull(_routeManager);
            
            _pendingTrainPrefab.SetActive(_routeManager.HasPendingTrainAtActiveScene());
            _routeManager.m_onPausedRouteRemoved += DisablePendingTrain;
        }

        private void OnDestroy()
        {
            _routeManager.m_onPausedRouteRemoved -= DisablePendingTrain;
        }


        private void DisablePendingTrain()
        {
            _pendingTrainPrefab.SetActive(false);
        }
    }
}
