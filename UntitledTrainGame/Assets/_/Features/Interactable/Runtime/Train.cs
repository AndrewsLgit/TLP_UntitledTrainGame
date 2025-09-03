using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Interactable.Runtime
{
    public class Train : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        private GameTime _timeToInteract;
        private RouteManager _routeManager;
        [SerializeField] private TrainRoute_Data _trainRoute;
        [SerializeField] private GameObject _regularTrainModel;
        [SerializeField] private GameObject _expressTrainModel;
        
        private bool _isExpress;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public GameTime TimeToInteract => _timeToInteract;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _routeManager = RouteManager.Instance;

            _isExpress = _trainRoute.IsExpress;
            
            // enable train model based on express train flag
            _regularTrainModel.SetActive(!_isExpress);
            _expressTrainModel.SetActive(_isExpress);
            
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            _routeManager.StartJourney(_trainRoute, _trainRoute.Network);
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}