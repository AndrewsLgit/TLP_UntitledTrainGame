using System;
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
        [SerializeField] private bool _isPendingTrain = false;
        
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
            SetModel();
            // _regularTrainModel.SetActive(!_isExpress || _isPendingTrain);
            // _expressTrainModel.SetActive(_isExpress && !_isPendingTrain);
            
        }

        private void OnEnable()
        {
            _isExpress = _trainRoute.IsExpress;
            // enable train model based on express train flag
            SetModel();
            // _regularTrainModel.SetActive(!_isExpress || _isPendingTrain);
            // _expressTrainModel.SetActive(_isExpress && !_isPendingTrain);
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            if (!_isPendingTrain)
            {
                _routeManager.StartJourney(_trainRoute, _trainRoute.Network);
                return;
            }

            if (_isPendingTrain && _routeManager.HasPendingTrainAtActiveScene())
            {
                _routeManager.ResumeJourneyFromPausedStation();
                return;
            }
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }

        private void SetModel()
        {
            _routeManager = RouteManager.Instance;
            if (_routeManager == null)
            {
                Debug.LogError("RouteManager not found!");
                return;           
            }
            if (_isPendingTrain)
            {
                _regularTrainModel.SetActive(false);
                _expressTrainModel.SetActive(false);
                var isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
                _regularTrainModel.SetActive(isPendingExists);
                return;
            }
            _regularTrainModel.SetActive(!_isExpress);
            _expressTrainModel.SetActive(_isExpress);
        }
        #endregion
    }
}