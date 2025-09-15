using System;
using System.Collections;
using System.Collections.Generic;
using Foundation.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using ToServiceInterfacesols.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameStateManager.Runtime
{

    // High-Level game states. These should be few but meaningful.
    public enum GameState
    {
        Exploring, // Player walking/interacting
        Traveling, // Route active, segment timers running, map visible
        PausedAtStation, // Journey paused at intermediate station
        InMenu, // Menu, dialog or map open
        Transitioning, // Transitioning between scenes
        LoopEnd // Loop end, bootstrap/start scene will be handled by SceneManager
    }
        
    // Small contract to allow swapping/mocking if we ever need to
    public interface IGameStateMachine
    {
        GameState Current { get; }
        event Action<GameState, GameState> OnStateChanged;
            
        // Requests coming from gameplay or UI
        void RequestStartJourney(TrainRoute_Data trainRoute, StationNetwork_Data stationNetwork);
        void RequestStartJourney(Station_Data start, Station_Data end, StationNetwork_Data stationNetwork);
        //void RequestPauseAtStation(int stationIndex);
        void RequestStopJourneyEarly(); // Request to stop at next station
        void RequestResumeJourney();
        void RequestGamePause();
        void RequestGameResume();
        void RequestGameExit();
        void RequestShowMap();
        void RequestHideMap();

        // Time requests (bench/clock UI)
        void RequestSleepToLoopEnd();
        void RequestWaitUntilNextEvent(string tag); // Train or empty string
            
        // Notifications coming from systems (Route/Clock/etc.)
        void NotifySegmentStarted(int segmentIndex, CountdownTimer timer);
        void NotifySegmentEnded();
        void NotifyJourneyStarted();
        void NotifyJourneyEnded();
        void NotifyLoopEnded();
    }
    
    [DefaultExecutionOrder(-500)]
    public class GameStateMachine : FMono, IGameStateMachine
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        
        private IClockService _clockService;
        private IRouteService _routeService;
        private IInputService _inputService;
        private IUiService _uiService;
        private ISceneService _sceneService;

        private string _sceneToLoad;
        private bool _isReady= false;
        private bool _eventsSubscribed = false;
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        public static GameStateMachine Instance { get; private set; }
        public GameState Current { get; private set; } = GameState.Exploring;
        public event Action<GameState, GameState> OnStateChanged = delegate { };
        
        // --- End of Public Variables --- 
        #endregion

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
            // Initialize asynchronously: wait until services are registered, then subscribe.
            StartCoroutine(InitializeAsync());
        }

        private IEnumerator InitializeAsync()
        {
            // Wait up to ~3 seconds for services to be registered
            const int maxFrames = 180;
            int frames = 0;

            while (frames < maxFrames)
            {
                // Try to resolve all services
                bool allResolved =
                    ServiceRegistry.TryResolve(out _sceneService) &&
                    ServiceRegistry.TryResolve(out _clockService) &&
                    ServiceRegistry.TryResolve(out _uiService) &&
                    ServiceRegistry.TryResolve(out _routeService) &&
                    ServiceRegistry.TryResolve(out _inputService);

                if (allResolved)
                {
                    _isReady = true;
                    SubscribeEvents();
                    yield break;
                }

                frames++;
                yield return null;
            }

            Error("GameStateMachine failed to initialize: services not available after waiting. " +
                  "Ensure ServicesBootstrapper registers services in the first scene.");
        }


        private void Update() { }

        private void FixedUpdate() { }

        private void OnEnable()
        {
            if (_isReady)
                SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }
        // Subscribe only when services are present
        private void SubscribeEvents()
        {
            if (_eventsSubscribed) return;

            if (_routeService == null || _clockService == null)
            {
                Warning("SubscribeEvents called before services are ready.");
                return;
            }

            _routeService.OnJourneyStarted += NotifyJourneyStarted;
            _routeService.OnSegmentStarted += NotifySegmentStarted;
            _routeService.OnSegmentEnded += NotifySegmentEnded;
            _routeService.OnDestinationChanged += NotifyDestinationChanged;
            _routeService.OnJourneyEnded += NotifyJourneyEnded;
            _clockService.OnLoopEnd += NotifyLoopEnded;

            _eventsSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (!_eventsSubscribed) return;

            // Guard each unsubscribe in case services got torn down first
            if (_routeService != null)
            {
                _routeService.OnJourneyStarted -= NotifyJourneyStarted;
                _routeService.OnSegmentStarted -= NotifySegmentStarted;
                _routeService.OnSegmentEnded -= NotifySegmentEnded;
                _routeService.OnDestinationChanged -= NotifyDestinationChanged;
                _routeService.OnJourneyEnded -= NotifyJourneyEnded;
            }

            if (_clockService != null)
            {
                _clockService.OnLoopEnd -= NotifyLoopEnded;
            }

            _eventsSubscribed = false;
        }


        private void OnDestroy() { }

        

        #endregion
        #region Requests (from gameplay/UI)
        
        public void RequestStartJourney(TrainRoute_Data route, StationNetwork_Data network)
        {
            if (route == null || network == null)
            {
                Warning("RequestStartJourney ignored: route or network is null.");
                return;
            }

            // Transition to Traveling and hint UI/input to switch
            ChangeState(GameState.Traveling);
            
            Assert.IsNotNull(_uiService, "UI service not found!");
            Assert.IsNotNull(_inputService, "Input service not found!");
            Assert.IsNotNull(_routeService, "Route service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.ShowMap();
            _inputService.SwitchToUI();
            _routeService.StartJourney(route, network);
            
            // _sceneToLoad = route.EndStation.StationScene.SceneName;
        }

        public void RequestStartJourney(Station_Data start, Station_Data end, StationNetwork_Data network)
        {
            if (start == null || end == null || network == null)
            {
                Warning("RequestStartJourney(start,end) ignored: invalid args.");
                return;
            }

            ChangeState(GameState.Traveling);
            
            Assert.IsNotNull(_uiService, "UI service not found!");
            Assert.IsNotNull(_inputService, "Input service not found!");
            Assert.IsNotNull(_routeService, "Route service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.ShowMap();
            _inputService.SwitchToUI();
            _routeService.StartJourney(start, end, network);
            
            // _sceneToLoad = end.StationScene.SceneName;
        }

        public void RequestResumeJourney()
        {
            // Toggle back to Traveling; RouteManager will handle resuming
            ChangeState(GameState.Traveling);
            
            Assert.IsNotNull(_uiService, "UI service not found!");
            Assert.IsNotNull(_inputService, "Input service not found!");
            Assert.IsNotNull(_routeService, "Route service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.ShowMap();
            _inputService.SwitchToUI();
            _routeService.ResumeJourneyFromPausedStation();
        }

        public void RequestStopJourneyEarly()
        {
            // RouteManager should listen and set its internal _stopEarly flag
            // We reuse OnSegmentEnded/RequestPauseAtStation pathways when applicable.
            // Consider adding a dedicated event if needed.
            ChangeState(GameState.Transitioning);
            Assert.IsNotNull(_routeService, "Route service not found!");
            _routeService.StopJourneyEarly();
            _uiService.ResetTravelUiState();
            _uiService.HideMap();
            _inputService.SwitchToPlayer();
            Info("Requested to stop journey early at next station.");
        }

        public void RequestGamePause()
        {
            // Let your Pause UI react to a separate channel if you want, or fold into state.
            // Keeping state unchanged here helps keep "paused" orthogonal to main game states.
            ChangeState(GameState.InMenu);
            Assert.IsNotNull(_uiService, "UI service not found!");
            
            _uiService.PauseGame();
            Info("Game pause requested.");
        }

        public void RequestGameResume()
        {
            ChangeState(GameState.Exploring);
            Assert.IsNotNull(_uiService, "UI service not found!");
            
            _uiService.ResumeGame();
            Info("Game resume requested.");
        }


        public void RequestGameExit()
        {
            Application.Quit();
        }

        public void RequestShowMap()
        {
            ChangeState(GameState.InMenu);
            Assert.IsNotNull(_uiService, "UI service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.ShowMap();
            _inputService.SwitchToUI();
            Info("Map requested.");
        }

        public void RequestHideMap()
        {
            ChangeState(GameState.Exploring);
            Assert.IsNotNull(_uiService, "UI service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.HideMap();
            _inputService.SwitchToPlayer();
            Info("Map hide requested.");
        }

        public void RequestSleepToLoopEnd()
        {
            ChangeState(GameState.Transitioning);
            Assert.IsNotNull(_clockService, "Clock service not found!");
            _clockService.SleepToLoopEnd();
        }

        public void RequestWaitUntilNextEvent(string tag)
        {
            Assert.IsNotNull(_clockService, "Clock service not found!");
            
            _clockService.FindNextEventWithTag(tag ?? string.Empty);
            _routeService.RemovePausedRoute();
        }

        public void RequestPreloadAndActivateScene()
        {
            ChangeState(GameState.Exploring);
            Assert.IsNotNull(_sceneService, "Scene service not found!");
            
            _sceneService.PreloadScene(_sceneToLoad);
            _sceneService.ActivateScene();
        }

        #endregion

        #region Notifications (from systems)

        public void NotifySegmentStarted(int index, CountdownTimer timer)
        {
            if (Current != GameState.Traveling)
                Info($"Segment started while state={Current}. Proceeding.");

            Assert.IsNotNull(_uiService, "UI Service not found!");
            _uiService.StartMapSegmentProgress(index, timer);
        }

        public void NotifySegmentEnded()
        {
            throw new NotImplementedException();
        }

        public void NotifySegmentEnded(GameTime time = default)
        {
            Assert.IsNotNull(_clockService, "Clock service not found!");
            _clockService.AdvanceTime(time);
        }

        public void NotifyJourneyStarted()
        {
            throw new NotImplementedException();
        }

        public void NotifyJourneyStarted(List<Station_Data> stationSegments = null)
        {
            // can be used to confirm ui state
            Assert.IsNotNull(_uiService, "UI service not found!");
            Assert.IsNotNull(stationSegments, "Station segments not found!");
            
            // _uiService.ResetTravelUiState();
            _uiService.ShowMap();
            _uiService.CreateProgressBarsForRoute(stationSegments);
            _inputService.SwitchToUI();
            
            // load last station scene on notification
            _sceneToLoad = stationSegments[^1].StationScene.SceneName;
        }

        public void NotifyJourneyEnded()
        {
            // When a journey ends, we transition back to Exploring and ask UI/input to restore
            ChangeState(GameState.Exploring);
            Assert.IsNotNull(_uiService, "UI service not found!");
            
            _uiService.ResetTravelUiState();
            _uiService.HideMap();
            _inputService.SwitchToPlayer();
            
            RequestPreloadAndActivateScene();
            // _sceneToLoad = sceneName;
        }

        public void NotifyDestinationChanged(SceneReference sceneName)
        {
            // Assert.IsNotNull(_uiService, "UI service not found!");
            
            // _uiService.HideMap();
            // _inputService.SwitchToPlayer();
            
            _sceneToLoad = sceneName.SceneName;
        }

        public void NotifyLoopEnded()
        {
            // Loop end means we go back to Exploring (bootstrap/start scene will be handled by SceneManager)
            ChangeState(GameState.LoopEnd);
            
            Assert.IsNotNull(_uiService, "UI service not found!");
            _uiService.ResetTravelUiState();
            _uiService.HideMap();
            _inputService.SwitchToPlayer();
            _sceneToLoad = _sceneService.StartScene.name;
            RequestPreloadAndActivateScene();
        }

        #endregion

        #region Internals

        private void ChangeState(GameState next)
        {
            if (next == Current) return;

            var prev = Current;
            Current = next;

            Info($"Game state changed: {prev} -> {next}");
            OnStateChanged.Invoke(prev, next);
        }

        #endregion
    }
}