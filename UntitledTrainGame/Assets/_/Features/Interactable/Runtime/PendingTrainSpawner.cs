using ServiceInterfaces.Runtime;
using Services.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class PendingTrainSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _pendingTrainPrefab;
        private IRouteService _routeManager;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _routeManager = ServiceRegistry.Resolve<IRouteService>();
            _pendingTrainPrefab.SetActive(false);
            Assert.IsNotNull(_routeManager);
            
            _pendingTrainPrefab.SetActive(_routeManager.HasPendingTrainAtActiveScene());
            _routeManager.OnPausedRouteRemoved += DisablePendingTrain;
        }

        private void OnDestroy()
        {
            _routeManager.OnPausedRouteRemoved -= DisablePendingTrain;
        }


        private void DisablePendingTrain()
        {
            _pendingTrainPrefab.SetActive(false);
        }
    }
}
