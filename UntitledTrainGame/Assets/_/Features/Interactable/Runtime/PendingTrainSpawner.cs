using System;
using System.Linq;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class PendingTrainSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _pendingTrainPrefab;
        private Train[] _notPendingTrainsInSchedule;
        private IRouteService _routeManager;
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Awake()
        {
            _notPendingTrainsInSchedule = GetComponentsInChildren<Train>().Where(t => !t.GetComponent<Train>().IsPendingTrain).ToArray();
        }

        void Start()
        {
            _routeManager = ServiceRegistry.Resolve<IRouteService>();
            _pendingTrainPrefab.SetActive(false);
            Assert.IsNotNull(_routeManager);
            
            _pendingTrainPrefab.SetActive(_routeManager.HasPendingTrainAtActiveScene());
            foreach (var train in _notPendingTrainsInSchedule)
            {
                train.gameObject.SetActive(!_routeManager.HasPendingTrainAtActiveScene());
            }
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
