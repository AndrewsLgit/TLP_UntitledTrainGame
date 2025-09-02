using System;
using System.Collections.Generic;
using Foundation.Runtime;
using Game.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Manager.Runtime
{
    public class RouteManager : FMono
    {
        #region Variables

        #region Private
        // Private Variables
        
        // References
        [SerializeField] private StationNetwork_Data _stationNetwork;
        [SerializeField] private TrainRoute_Data _testTrainRoute;
        private GDControlPanel _controlPanel = null;
        private SceneManager _sceneManager;
        private SceneReference _sceneToLoad;

        private List<Station_Data> _segments = new List<Station_Data>();
        private int _currentStationIndex = 0;
        private float _compressionFactor = 0.02f;
        private CountdownTimer _currentSegmentTimer;
        private bool _isExpress = false;
        private bool _stopEarly = false;

        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public static RouteManager Instance { get; private set; }
        
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
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controlPanel = GDControlPanel.Instance;
            // _sceneManager = SceneManager.Instance;
            _currentStationIndex = 0;
            
            Assert.IsNotNull(_controlPanel, "ControlPanel not found! Please add it to the GameManager object!");
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
        }

        private void OnDestroy()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentSegmentTimer != null && _currentSegmentTimer.IsRunning)
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
            StartJourney(_testTrainRoute, _stationNetwork);
        }
        // calculate segments and start traveling through them
        public void StartJourney(TrainRoute_Data trainRoute, StationNetwork_Data stationNetwork)
        {
            if (_currentSegmentTimer is { IsRunning: true }) return;
            CleanupCurrentJourney();
            _stationNetwork = stationNetwork;
            // get all stations from route.start to route.end from StationGraphSO
            _segments = _stationNetwork.CalculatePath(trainRoute.StartStation, trainRoute.EndStation);
            if (_segments == null)
            {
                Error($"Path not calculated!");
                return;
            }
            Info($"Preloading Scene: {trainRoute.EndStation.StationScene}");
            _sceneToLoad = trainRoute.EndStation.StationScene;
            _sceneManager = GetSceneLoader();
            //_sceneManager.PreloadScene(_sceneToLoad);
            
            Info($"Starting journey from {_segments[0].GetStationName()} to {_segments[^1].GetStationName()}");
            
            // test
            UIManager.Instance?.CreateProgressBarsForRoute(_segments, _stationNetwork, _compressionFactor);
            //test
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
            
            var realTime = _stationNetwork.GetTravelTime(_segments[index], _segments[index + 1]);
            Info($"Real time: {realTime}");
            var uiTime = realTime.ToTotalMinutes() * _compressionFactor;
            Info($"UI time: {uiTime}");
            
            //test
            _currentSegmentTimer = new CountdownTimer(uiTime);
            _currentSegmentTimer.OnTimerStop += EndSegment;
            
            UIManager.Instance?.StartSegmentProgress(index, _currentSegmentTimer);
            
            _currentSegmentTimer.Start();
            //test
            // Removed because now the timer triggers the end of the segment
            //EndSegment();
        }
        
        private void EndSegment()
        {
            _currentStationIndex++;
            if (!_isExpress && _stopEarly)
            {
                ChangeDestination(_currentStationIndex);
                _stopEarly = false;
                return;
            }
            StartSegment(_currentStationIndex);
        }

        private void ChangeDestination(int index)
        {
            // _sceneManager.UnloadScene(_sceneToLoad);
            _sceneToLoad = _segments[index].StationScene;
            // _sceneManager.PreloadScene(_sceneToLoad);
            EndJourney();
        }

        private void EndJourney()
        {
            InfoDone($"Journey ended.");
            _sceneManager.PreloadScene(_sceneToLoad);
            //todo: make the Station_Data.isDiscovered = true;
            // once we arrive at the destination
            _segments[_currentStationIndex].IsDiscovered = true;
            //test
            UIManager.Instance?.ClearProgressBars();
            // _currentSegmentTimer.Stop();
            // timer stop is done in the uiManager
            _currentSegmentTimer = null;
            //test
            _sceneManager.ActivateScene();
        }

        public void StopJourneyEarly()
        {
            if (_currentSegmentTimer == null) return;
            if(_currentSegmentTimer.IsRunning)
                _stopEarly = true;
        }
        
        #endregion

        #region Utils

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
            
            UIManager.Instance?.ClearProgressBars();
            
            _segments.Clear();
            _currentStationIndex = 0;
        }
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            _compressionFactor = _controlPanel.CompressionFactor;
        }

        #endregion
    }
}