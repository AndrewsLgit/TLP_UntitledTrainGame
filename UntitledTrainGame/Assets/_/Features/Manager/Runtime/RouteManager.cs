using System;
using System.Collections.Generic;
using Foundation.Runtime;
using Game.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;

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
        private GDControlPanel _controlPanel;
        private SceneLoader _sceneLoader;

        private List<Station_Data> _segments = new List<Station_Data>();
        private int _currentStationIndex = 0;
        private float _compressionFactor = 0.05f;
        private CountdownTimer _currentSegmentTimer;

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
            // _sceneLoader = SceneLoader.Instance;
            _currentStationIndex = 0;
        }

        // Update is called once per frame
        void Update()
        {
            //todo: remove update variable assignment
            _compressionFactor = _controlPanel.CompressionFactor;

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
            _currentStationIndex = 0;
            _segments.Clear();
            StartJourney(_testTrainRoute);
        }
        // calculate segments and start traveling through them
        public void StartJourney(TrainRoute_Data trainRoute)
        {
            CleanupCurrentJourney();
            // get all stations from route.start to route.end from StationGraphSO
            _segments = _stationNetwork.CalculatePath(trainRoute.StartStation, trainRoute.EndStation);
            if (_segments == null)
            {
                Error($"Path not calculated!");
                return;
            }
            Info($"Preloading Scene: {trainRoute.EndStation.StationScene}");
            _sceneLoader = GetSceneLoader();
            _sceneLoader.PreloadScene(trainRoute.EndStation.StationScene);
            
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
            var uiTime = realTime * _compressionFactor;
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
            StartSegment(_currentStationIndex);
        }

        private void EndJourney()
        {
            InfoDone($"Journey ended.");
            //test
            UIManager.Instance?.ClearProgressBars();
            // _currentSegmentTimer.Stop();
            // timer stop is done in the uiManager
            _currentSegmentTimer = null;
            //test
            _sceneLoader.ActivateScene();
        }
        
        #endregion

        #region Utils

        private SceneLoader GetSceneLoader()
        {
            if (_sceneLoader == null)
            {
                _sceneLoader = SceneLoader.Instance;
                if(_sceneLoader == null) Error("SceneLoader not found!");
            }
            return _sceneLoader;
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

        #endregion
    }
}
