using System;
using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Train : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        private float _timeToInteract;
        private SceneManager _sceneManager;
        private RouteManager _routeManager;
        [SerializeField] private TrainRoute_Data _trainRoute;
        
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public float TimeToInteract => _timeToInteract;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _sceneManager = SceneManager.Instance;
            _routeManager = RouteManager.Instance;
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            _routeManager.StartJourney(_trainRoute);
        }
        #endregion
    }
}