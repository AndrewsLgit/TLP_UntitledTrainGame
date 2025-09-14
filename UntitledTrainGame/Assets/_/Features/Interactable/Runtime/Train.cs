using Foundation.Runtime;
using GameStateManager.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
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
        private IRouteService _routeManager;
        private GameStateMachine _gameStateManager;
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
        public bool IsPendingTrain => _isPendingTrain;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _routeManager = ServiceRegistry.Resolve<IRouteService>();
            _gameStateManager = GameStateMachine.Instance;

            // _isExpress = _trainRoute.IsExpress;
            _isExpress = _trainRoute != null && _trainRoute.IsExpress;
            //_isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
            
            // enable train model based on express train flag
            SetModel();
            
            // _routeManager.OnPausedRouteRemoved += DisableModel;
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
                _routeManager = ServiceRegistry.Resolve<IRouteService>();
            
            Assert.IsNotNull(_routeManager);
            
            _isExpress = _trainRoute != null && _trainRoute.IsExpress;
            _routeManager.OnPausedRouteRemoved += DisableModel;

            //_isPendingExists = _routeManager.HasPendingTrainAtActiveScene();
            // enable train model based on express train flag
            SetModel();
            // _regularTrainModel.SetActive(!_isExpress || _isPendingTrain);
            // _expressTrainModel.SetActive(_isExpress && !_isPendingTrain);
        }
        private void OnDisable()
        {
            _routeManager.OnPausedRouteRemoved -= DisableModel;
        }

        private void OnDestroy()
        {
            // _routeManager.OnPausedRouteRemoved -= DisableModel;
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            if (_routeManager.HasPendingTrainAtActiveScene())
            {
                // _routeManager.ResumeJourneyFromPausedStation();
                _gameStateManager.RequestResumeJourney();
                return;
            }
            
            Assert.IsNotNull(_trainRoute);
            // _routeManager.StartJourney(_trainRoute, _trainRoute.Network);
            _gameStateManager.RequestStartJourney(_trainRoute, _trainRoute.Network);
            // if (!_isPendingTrain)
            // {
            //     Assert.IsNotNull(_trainRoute);
            //     _routeManager.StartJourney(_trainRoute, _trainRoute.Network);
            //     return;
            // }
            //
            // if (_isPendingTrain && _routeManager.HasPendingTrainAtActiveScene())
            // {
            //     _routeManager.ResumeJourneyFromPausedStation();
            //     return;
            // }
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
            _routeManager = ServiceRegistry.Resolve<IRouteService>();
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