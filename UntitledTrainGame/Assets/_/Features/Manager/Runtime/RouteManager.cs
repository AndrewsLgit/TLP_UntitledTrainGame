using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using Game.Runtime;
using ServiceInterfaces.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.Assertions;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Manager.Runtime
{
    public class RouteManager : FMono, IRouteService
    {
        #region Variables

        #region Private
        // Private Variables
        
        // References
        [SerializeField] private StationNetwork_Data _stationNetwork;
        [SerializeField] private TrainRoute_Data _trainRoute;
        [Header("Debug")]
        [SerializeField] private StationNetwork_Data[] _allStationNetworks;
        private GDControlPanel _controlPanel = null;
        private SceneManager _sceneManager;
        private SceneReference _sceneToLoad;

        private List<Station_Data> _segments = new List<Station_Data>();
        private int _currentStationIndex = 0;
        private float _compressionFactor = 0.02f;
        private float _minTravelTime = 5f;
        private CountdownTimer _currentSegmentTimer;
        private bool _isExpress = false;
        private bool _stopEarly = false;
        private GameTime _segmentTime;

        private bool _routePaused = false;
        private int _routePausedIndex = -1;

        private HashSet<Station_Data> _discoveredStations = new HashSet<Station_Data>();

        private readonly string _stationFacts = "DiscoveredStations";

        private bool _reloaded = false;

        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public static RouteManager Instance { get; private set; }
        public event Action<int, CountdownTimer> OnSegmentStarted;
        public event Action<List<Station_Data>> OnJourneyStarted;
        public event Action<GameTime> OnSegmentEnded;
        public event Action<SceneReference> OnDestinationChanged;
        public event Action OnJourneyEnded;
        public event Action OnPausedRouteRemoved;
        public event Action<Station_Data> OnTrainStationDiscovered;
        public event Action OnDiscoveredTrainStationsUpdated;
        
        public string StationFacts => _stationFacts;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            // Find instance of this class, if existent -> destroy that instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                Error("There is already an instance of this class! Destroying this one!");
                return;
            }

            // Assign instance as this current object
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SaveAllVisibleStations();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controlPanel = GDControlPanel.Instance;
            _currentStationIndex = 0;
            
            // if (!FactExists(_stationFacts, out _discoveredStations))
            // {
            //     _discoveredStations = new HashSet<Station_Data>();
            //     SetFact(_stationFacts, _discoveredStations, false);
            // }

        }

        private void OnEnable()
        {
            if (_controlPanel == null) _controlPanel = GDControlPanel.Instance;
            
            // Assert.IsNotNull(_controlPanel, "ControlPanel not found! Please add it to the GameManager object!");
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
        }

        private void OnDisable()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentSegmentTimer is { IsRunning: true })
            {
                _currentSegmentTimer.Tick(Time.deltaTime);
            }
        }
        #endregion
        
        #region Main Methods

        [ContextMenu("Start Test Journey")]
        private void StartJourney()
        {
            _segments = new List<Station_Data>();
            _currentStationIndex = 0;
            _segments.Clear();
            StartJourney(_trainRoute, _stationNetwork);
        }
        // calculate segments and start traveling through them
        public void StartJourney(TrainRoute_Data trainRoute, StationNetwork_Data stationNetwork)
        {
            if (_currentSegmentTimer is { IsRunning: true }) return;
            CleanupCurrentJourney();
            //RemovePausedRoute();
            _stationNetwork = stationNetwork;
            
            // get all stations from route.start to route.end from StationGraphSO
            _segments = _stationNetwork.CalculatePath(trainRoute.StartStation, trainRoute.EndStation);
            _segments[0].IsDiscovered = true;
            
            foreach (var stationData in _segments.FindAll(x => x.IsDiscovered)
                         .Where(x => !_discoveredStations.Contains(x)))
                _discoveredStations.Add(stationData);
            
            if (FactExists(_stationFacts, out _discoveredStations))
            {
                SetFact(_stationFacts, _discoveredStations, false);
            }

            if (_segments == null)
            {
                Error($"Path not calculated!");
                return;
            }
            Info($"Preloading Scene: {trainRoute.EndStation.StationScene}");
            
            Info($"Starting journey from {_segments[0].GetStationName()} to {_segments[^1].GetStationName()}");
            
            // test
            // UIManager.Instance?.CreateProgressBarsForRoute(_segments, _stationNetwork, _compressionFactor);
            OnJourneyStarted?.Invoke(_segments);
            
            // UIManager.Instance?.CreateProgressBarsForRoute(_segments);
            // UIManager.Instance?.ShowMap();
            // CustomInputManager.Instance.SwitchToUI();
            //test
            
            _isExpress = trainRoute.IsExpress;
            StartSegment(_currentStationIndex);
        }
        // Overload to start a journey from the current station to a target station without needing a TrainRoute asset
        public void StartJourney(Station_Data start, Station_Data end, StationNetwork_Data stationNetwork)
        {
            if (_currentSegmentTimer is { IsRunning: true }) return;
            CleanupCurrentJourney();
            
            _stationNetwork = stationNetwork;

            if (_stationNetwork == null)
            {
                Error("StationNetwork is not set. Cannot start journey.");
                return;
            }

            _segments = _stationNetwork.CalculatePath(start, end);
            if (_segments == null)
            {
                Error("Path not calculated!");
                return;
            }

            // _sceneToLoad = end.StationScene;
            // _sceneManager = GetSceneLoader();

            Info($"Starting journey from {_segments[0].GetStationName()} to {_segments[^1].GetStationName()}");
            OnJourneyStarted?.Invoke(_segments);
            // UIManager.Instance?.CreateProgressBarsForRoute(_segments, _stationNetwork, _compressionFactor);
            // UIManager.Instance?.CreateProgressBarsForRoute(_segments);
            // UIManager.Instance?.ShowMap();
            // CustomInputManager.Instance.SwitchToUI();
            //
            _isExpress = false;
            StartSegment(_currentStationIndex);
        }


        private void StartSegment(int index)
        {
            InfoInProgress($"Starting segment {index} of {_segments.Count-1}");
            if (index >= _segments.Count-1)
            {
                EndJourney();
                return;
            }
            
            InfoInProgress($"Segment {_segments[index].GetStationName()} -> {_segments[index + 1].GetStationName()}");
            
            _segmentTime = _stationNetwork.GetTravelTime(_segments[index], _segments[index + 1]);
            Info($"Travel time between {_segments[index].GetStationName()} -> {_segments[index+1].GetStationName()}: {_segmentTime.ToString()}");
            
            if(_isExpress)
                _segmentTime = GameTime.FromTotalMinutes(_segmentTime.ToTotalMinutes()/2);
            
            Info($"Real time: {_segmentTime}");
            // var uiTime = Mathf.Max(5f, _segmentTime.ToTotalMinutes() * _compressionFactor);
            var compressionFactor = _trainRoute != null ? _trainRoute.CompressionFactor : _compressionFactor;
            var uiTime = Mathf.Max(_minTravelTime, _segmentTime.ToTotalMinutes() * compressionFactor);
            //if (_isExpress) uiTime /= 2;
            Info($"UI time: {uiTime}");
            
            //test
            _currentSegmentTimer = new CountdownTimer(uiTime);
            _currentSegmentTimer.OnTimerStop += EndSegment;
            
            // UIManager.Instance?.StartSegmentProgress(index, _currentSegmentTimer);
            // UIManager.Instance?.StartMapSegmentProgress(index, _currentSegmentTimer);
            OnSegmentStarted?.Invoke(index, _currentSegmentTimer);
            
            _currentSegmentTimer.Start();
            //test
            // Removed because now the timer triggers the end of the segment
            //EndSegment();
        }
        
        private void EndSegment()
        {
            _currentStationIndex++;
            // if(!_reloaded)
                // ClockManager.Instance.AdvanceTime(_segmentTime);
            
            OnSegmentEnded?.Invoke(_segmentTime);
                
            if (!_isExpress && _stopEarly && !_reloaded)
            {
                ChangeDestination(_currentStationIndex);
                _stopEarly = false;
                return;
            }
            // if (!_reloaded)
                StartSegment(_currentStationIndex);
            
            // if(_reloaded)
                // EndJourney();
        }

        private void ChangeDestination(int index)
        {
            // _sceneManager.UnloadScene(_sceneToLoad);
            _sceneToLoad = _segments[index].StationScene;
            OnDestinationChanged?.Invoke(_sceneToLoad);
            // _sceneManager.PreloadScene(_sceneToLoad);
            
            ArriveAndPauseAtStation(index);
            // EndJourney();
        }

        // Pauses the route at the given station index, loads and activates the scene,
        // but does NOT clear the route segments. The train is considered "present" at this station.
        private void ArriveAndPauseAtStation(int stationIndex)
        {
            _routePaused = true;
            _routePausedIndex = stationIndex;
            
            //Mark discovered on arrival
            _segments[stationIndex].IsDiscovered = true;
            OnTrainStationDiscovered?.Invoke(_segments[stationIndex]);
            
            // Clear any running UI/timers for the traveling segment
            if (_currentSegmentTimer != null)
            {
                _currentSegmentTimer.OnTimerStop -= EndSegment;
                _currentSegmentTimer.Stop();
                _currentSegmentTimer = null;
            }
            // UIManager.Instance?.ResetInternalState();
            // UIManager.Instance?.HideMap();
            // CustomInputManager.Instance?.SwitchToPlayer();
            
            // Load and activate the scene
            // _sceneManager = GetSceneLoader();
            // _sceneManager.PreloadScene(_sceneToLoad);
            // _sceneManager.ActivateScene();
            OnJourneyEnded?.Invoke();
            
            InfoDone($"Journey paused at station {_segments[stationIndex].GetStationName()}");
        }

        private void EndJourney()
        {
            InfoDone($"Journey ended.");
            // if(!_reloaded)
            //     _sceneManager.PreloadScene(_sceneToLoad);
            //todo: make the Station_Data.isDiscovered = true;
            // once we arrive at the destination
            _segments[_currentStationIndex].IsDiscovered = true;
            // _discoveredStations = _segments.FindAll(x => x.IsDiscovered);
            //todo: turn this into a proper method
            if (FactExists(_stationFacts, out _discoveredStations) &&
                _discoveredStations.Add(_segments[_currentStationIndex]))
            {
                SetFact(_stationFacts, _discoveredStations, false);
            }
            // _discoveredStations.Add(_segments[_currentStationIndex]);
            // SetFact(_stationFacts, _discoveredStations, false);
            
            OnTrainStationDiscovered?.Invoke(_segments[_currentStationIndex]);
            //test
            // UIManager.Instance?.ResetTravelUiState();
            // UIManager.Instance?.HideMap();
            // CustomInputManager.Instance?.SwitchToPlayer();
            // _currentSegmentTimer.Stop();
            // timer stop is done in the uiManager
            _currentSegmentTimer = null;
            _isExpress = false;
            //test
            // if(!_reloaded)
            //     _sceneManager.ActivateScene();
            OnJourneyEnded?.Invoke();
            
            RemovePausedRoute();
        }

        private void EndJourneyNoLoad()
        {
            InfoDone($"Journey ended.");
            if (FactExists(_stationFacts, out _discoveredStations) &&
                _discoveredStations.Add(_segments[_currentStationIndex]))
            {
                SetFact(_stationFacts, _discoveredStations, false);
            }
            UIManager.Instance?.ResetTravelUiState();
            UIManager.Instance?.HideMap();
            CustomInputManager.Instance?.SwitchToPlayer();
            _currentSegmentTimer = null;
            _isExpress = false;
            RemovePausedRoute();
        }

        public void StopJourneyEarly()
        {
            if (_currentSegmentTimer == null) return;
            if(_currentSegmentTimer.IsRunning)
                _stopEarly = true;
        }
        
        #endregion

        #region Utils
        
        // True if a paused route is present and the active scene is the paused station's scene
        public bool HasPendingTrainAtActiveScene()
        {
            if(!_routePaused || _routePausedIndex < 0 || _routePausedIndex >= _segments.Count)
                return false;

            var activeSceneName = UnitySceneManager.GetActiveScene().name;
            var pausedSceneName = _segments[_routePausedIndex].StationScene?.SceneName;
            Info($"pausedSceneName: {pausedSceneName}, activeSceneName: {activeSceneName}");
            // return !string.IsNullOrEmpty(pausedSceneName) && string.Equals(activeSceneName, pausedSceneName, StringComparison.Ordinal);
            return _routePaused;
        }
        // Resume the route from the paused station, continuing to the next station if available
        public void ResumeJourneyFromPausedStation()
        {
            if (!_routePaused)
            {
                Warning("No paused journey to resume.");
                return;
            }
            
            var startStation = _segments[_routePausedIndex];
            var endStation = _segments[^1];
            
            // Ensure current index is set to the paused station
            _currentStationIndex = _routePausedIndex;
            // _trainRoute = null;
            // _routePaused = false;
            // _routePausedIndex = -1;
            
            RemovePausedRoute();
            
            // Recreate route UI
            //UIManager.Instance?.CreateProgressBarsForRoute(_segments, _stationNetwork, _compressionFactor);
            
            // If we are at the final station already, end the journey; otherwise, start the next segment
            if (_currentStationIndex >= _segments.Count - 1)
            {
                EndJourney();
            }
            else
            {
                // StartSegment(_currentStationIndex);
                StartJourney(startStation, endStation, _stationNetwork);
            }
            
        }
        
        public void RemovePausedRoute()
        {
            _trainRoute = null;
            _routePaused = false;
            _routePausedIndex = -1;
            _reloaded = false;

            OnPausedRouteRemoved?.Invoke();
        }

        private SceneManager GetSceneLoader()
        {
            if (_sceneManager == null)
            {
                _sceneManager = SceneManager.Instance;
                if(_sceneManager == null) Error("SceneManager not found!");
            }
            return _sceneManager;
        }

        private void CleanupCurrentJourney()
        {
            if (_currentSegmentTimer != null)
            {
                _currentSegmentTimer.OnTimerStop -= EndSegment;
                _currentSegmentTimer.Stop();
                _currentSegmentTimer = null;
            }
            
            // UIManager.Instance?.ResetTravelUiState();
            
            _segments.Clear();
            _currentStationIndex = 0;
        }
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            _compressionFactor = _controlPanel.CompressionFactor;
            _minTravelTime = _controlPanel.MinTravelTime;
        }

        #endregion
        
        #region Debug

        [ContextMenu("Remove all visibility (except for A3)")]
        private void RemoveAllVisibility()
        {
            foreach (var stationNetwork in _allStationNetworks)
            {
                foreach (var station in stationNetwork.Connections)
                {
                    if((station.From.LinePrefix == StationPrefix.A && station.From.Id == 3)
                       || (station.To.LinePrefix == StationPrefix.A && station.To.Id == 3)) continue;
                    station.From.IsDiscovered = false;
                    station.To.IsDiscovered = false;
                }
            }
            
            SaveAllVisibleStations();
        }
        
        [ContextMenu("Make every station visible")]
        private void MakeAllVisible()
        {
            foreach (var stationNetwork in _allStationNetworks)
            {
                foreach (var station in stationNetwork.Connections)
                {
                    station.From.IsDiscovered = true;
                    station.To.IsDiscovered = true;
                }
            }
            
            SaveAllVisibleStations();
        }

        private void SaveAllVisibleStations()
        {
            if (!FactExists(_stationFacts, out _discoveredStations))
            {
                _discoveredStations = new HashSet<Station_Data>();
                SetFact(_stationFacts, _discoveredStations, false);
            }
            foreach (var stationNetwork in _allStationNetworks)
            {
                foreach (var station in stationNetwork.Connections.Where(x => x.From.IsDiscovered || x.To.IsDiscovered))
                {
                    // if((station.From.LinePrefix == StationPrefix.A && station.From.Id == 3)
                    //    || (station.To.LinePrefix == StationPrefix.A && station.To.Id == 3)) continue;
                    // station.From.IsDiscovered = false;
                    // station.To.IsDiscovered = false;
                    //
                    if(station.From.IsDiscovered)
                        _discoveredStations.Add(station.From);
                    if(station.To.IsDiscovered)
                        _discoveredStations.Add(station.To);
                }
            }
            
            OnDiscoveredTrainStationsUpdated?.Invoke();
        }
        #endregion
    }
}