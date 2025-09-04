using System;
using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

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
        private bool _isPendingExists = false;
        
        private bool _isExpress;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public GameTime TimeToInteract => _timeToInteract;
        public InteractionType InteractionType => InteractionType.Train;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _routeManager = RouteManager.Instance;

            // _isExpress = _trainRoute.IsExpress;
            _isExpress = _trainRoute != null && _trainRoute.IsExpress;
            //_isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
            
            // enable train model based on express train flag
            SetModel();
            
            _routeManager.m_onPausedRouteRemoved += DisableModel;
            // _regularTrainModel.SetActive(!_isExpress || _isPendingTrain);
            // _expressTrainModel.SetActive(_isExpress && !_isPendingTrain);
            
        }

        private void Update()
        {
            if (!_isPendingTrain) return;
            // if (!_routeManager.HasPendingTrainAtActiveScene()) 
            //SetModel();
            // if (FactExists<bool>("isPending", out var isPending))
            // {
            //     GetFact<bool>("isPending");
            //     _isPendingTrain = isPending;
            //     SetModel();
            // }
        }

        private void OnEnable()
        {
            if(_routeManager == null)
                _routeManager = RouteManager.Instance;
            
            Assert.IsNotNull(_routeManager);
            
            _isExpress = _trainRoute != null && _trainRoute.IsExpress;
            //_isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
            // enable train model based on express train flag
            SetModel();
            // _regularTrainModel.SetActive(!_isExpress || _isPendingTrain);
            // _expressTrainModel.SetActive(_isExpress && !_isPendingTrain);
        }

        private void OnDestroy()
        {
            _routeManager.m_onPausedRouteRemoved -= DisableModel;
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            if (!_isPendingTrain)
            {
                Assert.IsNotNull(_trainRoute);
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

        private void DisableModel()
        {
            _regularTrainModel.SetActive(false);
            _expressTrainModel.SetActive(false);
            gameObject.SetActive(false);
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
                Info($"Train is pending.");
                _regularTrainModel.SetActive(false);
                _expressTrainModel.SetActive(false);
                _isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
                Info($"Pending train exists in scene: {_isPendingExists}");
                _regularTrainModel.SetActive(_isPendingExists);
                return;
            }
            _regularTrainModel.SetActive(!_isExpress);
            _expressTrainModel.SetActive(_isExpress);
        }
        #endregion
    }
}