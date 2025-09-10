using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using TMPro;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Game.Runtime
{
    public class UIManager : FMono
    {
        #region Variables

        #region Private
        // Private Variables
        
        private bool _isPaused;

        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private GameObject _trainTravelUI;
        [SerializeField] private GameObject _gameOverMenu;

        [Header("Progress Bar")] 
        [SerializeField] private Transform _progressBarsParent;
        [SerializeField] private GameObject _progressBarPrefab;

        [Header("Main Map UI")]
        [SerializeField] Transform _mainMapUIParent;
        [Header("Layer Train Stations")]
        [SerializeField] private string _trainStationLayerName = "Layer_Stations";
        private HashSet<Transform> _trainStations;
        [SerializeField] private Transform[] _trains;
        [Header("Layer Station Lines")]
        [SerializeField] private string _stationLineLayerName = "Layer_Network";
        private HashSet<Transform> _stationPathLines;
        [SerializeField] private Transform[] _stationPathLinesList;
        [Header("Layer Travel Times")]
        [SerializeField] private string _travelTimeLayerName = "Layer_TravelTime";
        private HashSet<Transform> _travelTimes;
        [SerializeField] private Transform[] _travelTimesList;

        [Header("Map Clock")] 
        [SerializeField] private TextMeshProUGUI _clockMinuteUnit;
        [SerializeField] private TextMeshProUGUI _clockMinuteTens;
        [SerializeField] private TextMeshProUGUI _clockHourUnit;
        [SerializeField] private TextMeshProUGUI _clockHourTens;
        
        private List<GameObject> _progressBars = new List<GameObject>();
        private List<Slider> _progressBarsSliders = new List<Slider>();
        private CountdownTimer _currentTimer;
        private int _currentSegmentIndex = -1;
        private List<Station_Data> _segments = new List<Station_Data>();
        private List<Transform> _segmentsToLoad = new List<Transform>();

        // Private variables
        #endregion
        
        #region Public
        // Public Variables
        public static UIManager Instance { get; private set; }
        // Public Variables
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
            // pause game on awake (optional, can put inside Start() method
            //_isPaused = true;
            
            // some UI setup
        }

        private void Start()
        {
            SetupMapItems();
            
            Info($"UIManager started. Found {_trainStations.Count} stations and {_stationPathLines.Count} lines.");
            
            // Subscribe to clock event
            // ClockManager.Instance.m_OnTimeUpdated += UpdateClockTime;
            // UpdateClockTime(ClockManager.Instance.m_CurrentTime);
        }

        private void OnDestroy()
        {
            // ClockManager.Instance.m_OnTimeUpdated -= UpdateClockTime;
        }

        private void Update()
        {
            // don't execute logic if the game is paused
            if (_isPaused) return;
        }

        #endregion
        
        #region Main Methods

        #region Train Travel UI

        public void CreateProgressBarsForRoute(List<Station_Data> segments)
        {
            _segments = segments;

            DisableAllMapItems();
            _segmentsToLoad.Clear();
            
            _trainStations.FirstOrDefault(x => x.name.Contains("A3") /*segments[0].StationScene.name.Contains(x.name)*/).gameObject.SetActive(true);
            
            for (var i = 0; i < segments.Count - 1; i++)
            {
                var from = segments[i];
                var to = segments[i + 1];
                
                _segmentsToLoad.Add(
                    _stationPathLines.FirstOrDefault(x => x.name.Contains($"{from.GetStationName()}_{to.GetStationName()}") 
                                                              || x.name.Contains($"{to.GetStationName()}_{from.GetStationName()}")));
                
                Info($"Created path line for segment {segments[i].GetStationName()} -> {segments[i + 1].GetStationName()}");
            }
        }
        public void StartMapSegmentProgress(int segmentIndex, CountdownTimer timer)
        {
            // Unsub from previous timer
            if(_currentTimer != null) _currentTimer.OnTimerTick -= UpdateCurrentMapProgress;
            
            _currentSegmentIndex = segmentIndex;
            _currentTimer = timer;
            
            if(_currentTimer != null) _currentTimer.OnTimerTick += UpdateCurrentMapProgress;
                
            Info($"Started progress tracking for segment {segmentIndex}");
        }
        private void UpdateCurrentMapProgress(float progress)
        {
            // Update current segment progress (0 -> 1)
            if (_currentSegmentIndex >= 0 && _currentSegmentIndex < _progressBarsSliders.Count)
            {
                var bar = _segmentsToLoad[_currentSegmentIndex].GetComponent<Image>();
                    bar.fillAmount = 1f - progress;
                InfoInProgress($"Timer progress: {progress:P0}");
            }

            MarkCompletedMapSegments();
        }
        private void MarkCompletedMapSegments()
        {
            for (int i = 0; i < _currentSegmentIndex; i++)
            {
                if (i < _segmentsToLoad.Count)
                {
                    _segmentsToLoad[i].GetComponent<Image>().fillAmount = 1f;
                }
            }
        }
        
        

        public void CreateProgressBarsForRoute(List<Station_Data> segments, StationNetwork_Data network,
            float compressionFactor)
        {
            ClearProgressBars();

            for (var i = 0; i < segments.Count - 1; i++)
            {
                var from = segments[i];
                var to = segments[i + 1];

                GameObject progressBarObj = Instantiate(_progressBarPrefab, _progressBarsParent);
                Slider slider = progressBarObj.GetComponentInChildren<Slider>();
                TextMeshProUGUI label = progressBarObj.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    label.text = $"Station {from.GetStationName()} -> Station {to.GetStationName()}";
                }

                if (slider != null)
                {
                    slider.value = 0f;
                    var fillImage = slider.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        // fillImage.color = GetSegmentColor(i);
                        fillImage.color = Color.green;
                    }
                }

                progressBarObj.SetActive(true);
                _progressBars.Add(progressBarObj);
                _progressBarsSliders.Add(slider);
                
                Info($"Created progress bar for segment {segments[i].GetStationName()} -> {segments[i + 1].GetStationName()}");

                //_trainTravelUI.SetActive(true);
            }
        }

        public void StartSegmentProgress(int segmentIndex, CountdownTimer timer)
        {
            // Unsub from previous timer
            if(_currentTimer != null) _currentTimer.OnTimerTick -= UpdateCurrentProgress;
            
            _currentSegmentIndex = segmentIndex;
            _currentTimer = timer;
            
            if(_currentTimer != null) _currentTimer.OnTimerTick += UpdateCurrentProgress;
                
            Info($"Started progress tracking for segment {segmentIndex}");
        }
        
        private void UpdateCurrentProgress(float progress)
        {
            // Update current segment progress (0 -> 1)
            if (_currentSegmentIndex >= 0 && _currentSegmentIndex < _progressBarsSliders.Count)
            {
                _progressBarsSliders[_currentSegmentIndex].value = 1f - progress;
                InfoInProgress($"Timer progress: {progress:P0}");
            }

            MarkCompletedSegments();
        }
        

        private void MarkCompletedSegments()
        {
            for (int i = 0; i < _currentSegmentIndex; i++)
            {
                if (i < _progressBarsSliders.Count)
                {
                    _progressBarsSliders[i].value = 1f;
                }
            }
        }

        private Color GetSegmentColor(int segmentIndex)
        {
            Color[] colors = { 
                Color.blue, Color.green, Color.yellow, 
                Color.red, Color.magenta, Color.cyan 
            };
            return colors[segmentIndex % colors.Length];
        }

        public void ClearProgressBars()
        {
            foreach (var bar in _progressBars)
            {
                if (bar != null) Destroy(bar);
            }
            _progressBars.Clear();
            _progressBarsSliders.Clear();
            _currentSegmentIndex = -1;
            
            // if (_currentTimer != null)
            // {
            //     _currentTimer.OnTimerTick -= UpdateCurrentProgress;
            //     _currentTimer.Stop();
            //     _currentTimer = null;
            // }
            
            _currentTimer = null;
        }

        #endregion
        
        // Set (invert) pause state
        public void PauseGame()
        {
            // inverse _isPaused value
            _isPaused = !_isPaused;
            // set timescale to pause value
            Time.timeScale = _isPaused ? 0 : 1;
            // then activate the pause menu UI
            _pauseMenu.SetActive(_isPaused);
        }
        public void ResumeGame()
        {
            // remove paused state
            _isPaused = false;
            // set timeScale to 1 (game resumed)
            Time.timeScale = 1;
            // disable the pause menu UI
            _pauseMenu.SetActive(_isPaused );
        }

        public void QuitGame()
        {
            // exit game
            Application.Quit();
        }

        public void RestartGame()
        {
            // load first scene in SceneManager
            // todo: declare scene inside Unity Build Settings
            SceneManager.LoadSceneAsync(1);
        }

        public void GameOver()
        {
            // stop game time
            Time.timeScale = 0;
            _gameOverMenu.SetActive(true);
        }
        
        #endregion 
        
        #region Helper Methods

        private void SetupMapItems()
        {
            _trainStations = _mainMapUIParent.GetComponentsInChildren<Transform>()
                .Where(c => c.parent.name == _trainStationLayerName)
                .Select(c => c).ToHashSet();

            // _trains = _trainStations.ToArray();
            
            _stationPathLines = _mainMapUIParent.GetComponentsInChildren<Transform>()
                .Where(x => x.parent.name == _stationLineLayerName)
                .Select(c => c).ToHashSet();
            
            // _stationPathLinesList = _stationPathLines.ToArray();
            
            _travelTimes = _mainMapUIParent.GetComponentsInChildren<Transform>()
                .Where(c => c.parent.name == _travelTimeLayerName)
                .Select(c => c).ToHashSet();
            
            _trains = _trainStations.ToArray();
            _stationPathLinesList = _stationPathLines.ToArray();
            _travelTimesList = _travelTimes.ToArray();
        }

        private void DisableAllMapItems()
        {
            _stationPathLines.Select(x => x.gameObject).ToList().ForEach(x => x.SetActive(false));
            _travelTimes.Select(x => x.gameObject).ToList().ForEach(x => x.SetActive(false));
            _trainStations.Select(x => x.gameObject).ToList().ForEach(x => x.SetActive(false));
        }

        private void UpdateClockTime(GameTime time)
        {
            int[] numbers = time.Minutes.ToString()
                .Select(c => int.Parse(c.ToString()))
                .ToArray();
            
            _clockMinuteUnit.text = numbers[0].ToString();
            _clockMinuteTens.text = numbers[1].ToString();

            numbers = time.Hours.ToString()
                .Select(c => int.Parse(c.ToString()))
                .ToArray();
            _clockHourUnit.text = numbers[0].ToString();
            _clockHourTens.text = numbers[1].ToString();
        }
        #endregion
    }
}