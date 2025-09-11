using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private Transform _currentStationInMap;
        private string _currentStationLabel;
        private bool _isCurrentInverted = false;
        
        // Per-segment visual info to handle direction/orientation
        private struct SegmentPathVisual
        {
            public Transform Transform;
            public Image Image;
            public bool Inverted;
            public int OriginalFillOrigin;
            public bool OriginalFillClockwise;
        }

        private readonly List<SegmentPathVisual> _segmentVisuals = new List<SegmentPathVisual>();
        
        // Keep a local cache of discovered station labels so we re-apply visibility after bulk hides
        private readonly HashSet<string> _discoveredStationLabels = new HashSet<string>(StringComparer.Ordinal);

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
            DisableAllMapItems(); 
            
            LoadDiscoveredStationsFromFacts();
            RefreshDiscoveredStationsVisibility();
            Info($"UIManager started. Found {_trainStations.Count} stations and {_stationPathLines.Count} lines.");


            RouteManager.Instance.OnTrainStationDiscovered += OnTrainStationDiscovered;
            // Subscribe to clock event
            ClockManager.Instance.m_OnTimeUpdated += UpdateClockTime;
            UpdateClockTime(ClockManager.Instance.m_CurrentTime);
        }

        private void OnDestroy()
        {
            ClockManager.Instance.m_OnTimeUpdated -= UpdateClockTime;
            RouteManager.Instance.OnTrainStationDiscovered -= OnTrainStationDiscovered;
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
            
            // Reload discovered stations from facts
            LoadDiscoveredStationsFromFacts();
            RefreshDiscoveredStationsVisibility(); // Keep any discovered stations visible
            _segmentsToLoad.Clear();
            _segmentVisuals.Clear();
            var from = segments[0];
            var fromLabel = $"{from.LinePrefix}{from.Id}";
            
            // _currentStationInMap = _trainStations.FirstOrDefault(x =>
            //     x.name.Contains(fromLabel) /*segments[0].StationScene.name.Contains(x.name)*/);
            // _currentStationInMap.gameObject.SetActive(true);
            
            
            for (var i = 0; i < segments.Count - 1; i++)
            {
                var segFrom = segments[i];
                var segTo = segments[i + 1];

                fromLabel = $"{segFrom.LinePrefix}{segFrom.Id}";
                var toLabel = $"{segTo.LinePrefix}{segTo.Id}";
                
                // _isCurrentInverted = from.Id > to.Id;
                // var inverted = segFrom.Id > segTo.Id;
                
                var lineTransform = _stationPathLines.FirstOrDefault(x =>
                    (x.name.Contains($"{fromLabel}") && x.name.Contains($"{toLabel}")));
                
                // Local helpers to parse labels like "A12" into (prefix="A", number=12)
                (string prefix, int number) SplitLabel(string label)
                {
                    var m = Regex.Match(label, @"^([A-Za-z]+)(\d+)$");
                    return m.Success ? (m.Groups[1].Value, int.Parse(m.Groups[2].Value)) : (string.Empty, 0);
                }

                bool ComputeInverted(string lfName, string fromLbl, string toLbl, int fallbackFromId, int fallbackToId)
                {
                    if (string.IsNullOrEmpty(lfName)) return fallbackFromId > fallbackToId;

                    // Extract all labels embedded in the line transform name (e.g., "D2_A5_Path")
                    var labelMatches = Regex.Matches(lfName, @"[A-Za-z]+\d+")
                                            .Cast<Match>()
                                            .Select(m => m.Value)
                                            .Distinct()
                                            .ToList();
                    if (labelMatches.Count < 2)
                        return fallbackFromId > fallbackToId;

                    // If the line contains fromLabel, compare fromLabel with the other label that shares its prefix.
                    if (labelMatches.Contains(fromLbl))
                    {
                        var (pFrom, nFrom) = SplitLabel(fromLbl);

                        var other = labelMatches.FirstOrDefault(x => !string.Equals(x, fromLbl, StringComparison.Ordinal) && x.Contains(pFrom));
                        var (pOther, nOther) = SplitLabel(other ?? string.Empty);
                        if (!string.IsNullOrEmpty(pFrom) && pFrom == pOther)
                        {
                            // Example: if common prefix is 'D' and other is D5 => compare D2 > D5
                            return nFrom > nOther;
                        }
                    }

                    // If the line contains toLabel, compare the other label (with same prefix) against toLabel.
                    if (labelMatches.Contains(toLbl))
                    {
                        var (pTo, nTo) = SplitLabel(toLbl);
                        var other = labelMatches.FirstOrDefault(x => !string.Equals(x, toLbl, StringComparison.Ordinal) && x.Contains(pTo));
                        
                        var (pOther, nOther) = SplitLabel(other ?? string.Empty);
                        if (!string.IsNullOrEmpty(pTo) && pTo == pOther)
                        {
                            // Example: if common prefix is 'A' and other is A5 => compare A5 > A2
                            return nOther > nTo;
                        }
                    }

                    // Fallback to numeric comparison on segment ids if no suitable common-prefix comparison was found
                    return fallbackFromId > fallbackToId;
                }

                var inverted = ComputeInverted(lineTransform != null ? lineTransform.name : null, fromLabel, toLabel, segFrom.Id, segTo.Id);


                _segmentsToLoad.Add(lineTransform);
                
                
                var img = lineTransform != null ? lineTransform.GetComponent<Image>() : null;
                if (img == null)
                {
                    Warning($"No Image found for path line {fromLabel}->{toLabel}");
                }
                else
                {
                    // Ensure we start hidden
                    img.fillAmount = 0f;
                    lineTransform.gameObject.SetActive(false);
                }

                if (lineTransform != null)
                {
                    _segmentVisuals.Add(new SegmentPathVisual
                    {
                        Transform = lineTransform,
                        Image = img,
                        Inverted = inverted,
                        OriginalFillOrigin = img != null ? img.fillOrigin : 0,
                        OriginalFillClockwise = img != null && img.fillClockwise
                    });
                }

                
                Info($"Created path line for segment {fromLabel} -> {toLabel}");
            }
        }
        public void StartMapSegmentProgress(int segmentIndex, CountdownTimer timer)
        {
            // Unsub from previous timer
            if(_currentTimer != null) _currentTimer.OnTimerTick -= UpdateCurrentMapProgress;
            
            
            //_currentStationInMap.gameObject.SetActive(true); 
            
            _currentSegmentIndex = segmentIndex;
            _currentTimer = timer;
            
            // Prepare visual for this segment with proper direction/origin
            if (_currentSegmentIndex >= 0 && _currentSegmentIndex < _segmentVisuals.Count)
            {
                var vis = _segmentVisuals[_currentSegmentIndex];
                if (vis.Image != null)
                {
                    ApplyDirectionToImage(vis);
                    vis.Image.fillAmount = 0f;
                    vis.Transform.gameObject.SetActive(true);
                }
            }
            
            if(_currentTimer != null) _currentTimer.OnTimerTick += UpdateCurrentMapProgress;
                
            Info($"Started progress tracking for segment {segmentIndex}");
        }
        private void UpdateCurrentMapProgress(float progress)
        {
            // Update current segment progress (0 -> 1)
            if (_currentSegmentIndex >= 0 && _currentSegmentIndex < _segmentsToLoad.Count)
            {
                // var bar = _segmentsToLoad[_currentSegmentIndex].GetComponent<Image>();
                // if (_isCurrentInverted)
                //     bar.fillClockwise = true;
                // bar.fillAmount = 0f;
                // _segmentsToLoad[_currentSegmentIndex].gameObject.SetActive(true);
                // bar.fillAmount = 1f - progress;
                // InfoInProgress($"Timer progress: {progress:P0}");
                
                var vis = _segmentVisuals[_currentSegmentIndex];
                var bar = vis.Image;
                if (bar != null)
                {
                    // With origin/clockwise adjusted per direction, we can set fill directly
                    bar.fillAmount = Mathf.Clamp01(1f - progress);
                    InfoInProgress($"Timer progress: {progress:P0}");
                }

            }

            MarkCompletedMapSegments();
        }
        private void MarkCompletedMapSegments()
        {
            for (int i = 0; i < _currentSegmentIndex; i++)
            {
                if (i < _segmentsToLoad.Count)
                {
                    // _segmentsToLoad[i].GetComponent<Image>().fillAmount = 1f;
                    
                    _segmentVisuals[i].Image.fillAmount = 1f;
                    
                    // var currentStation = _segments[_currentSegmentIndex];
                    // _currentStationLabel = $"{currentStation.LinePrefix}{currentStation.Id}"; 
                    // _trainStations.FirstOrDefault(x => x.name.Contains(_currentStationLabel))
                    //     .gameObject.SetActive(true);
                }
            }
        }
        
        public void ClearProgressBars()
        {
            // foreach (var bar in _progressBars)
            // {
            //     if (bar != null) Destroy(bar);
            // }
            // _progressBars.Clear();
            // _progressBarsSliders.Clear();
            _currentSegmentIndex = -1;
            
            // if (_currentTimer != null)
            // {
            //     _currentTimer.OnTimerTick -= UpdateCurrentProgress;
            //     _currentTimer.Stop();
            //     _currentTimer = null;
            // }
            
            _currentTimer = null;
            _segmentVisuals.Clear();
            _segmentsToLoad.Clear();
            RefreshDiscoveredStationsVisibility();
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

            // if(FactExists<HashSet<Station_Data>>(RouteManager.Instance.StationFacts, out var discovered))
            // discovered = GetFact<HashSet<Station_Data>>(RouteManager.Instance.StationFacts);
            //
            // if (discovered == null) return;
            // {
            //     foreach (var label in discovered.Select(station => $"{station.LinePrefix}{station.Id}"))
            //     {
            //         _trainStations.FirstOrDefault(x => x.name.Contains(label))
            //             .gameObject.SetActive(true);
            //     }
            // }
            
            // Re-enable any discovered stations
            RefreshDiscoveredStationsVisibility();
        }

        private void UpdateClockTime(GameTime time)
        {
            int[] numbers = time.Minutes.ToString()
                .Select(c => int.Parse(c.ToString()))
                .ToArray();
            if (numbers.Length == 1)
            {
                numbers = new[] {0, numbers[0]};
            }
            
            _clockMinuteUnit.text = numbers[1].ToString();
            _clockMinuteTens.text = numbers[0].ToString();

            numbers = time.Hours.ToString()
                .Select(c => int.Parse(c.ToString()))
                .ToArray();
            if (numbers.Length == 1)
                numbers = new[] {0, numbers[0]};
            
            _clockHourUnit.text = numbers[1].ToString();
            _clockHourTens.text = numbers[0].ToString();
        }

        private void OnTrainStationDiscovered(Station_Data station)
        {
            _currentStationLabel = $"{station.LinePrefix}{station.Id}";
            _discoveredStationLabels.Add(_currentStationLabel);
            var t = _trainStations.FirstOrDefault(x => x.name.Contains(_currentStationLabel));
            if(t != null) t.gameObject.SetActive(true);
            // _trainStations.FirstOrDefault(x => x.name.Contains(_currentStationLabel))
            // .gameObject.SetActive(true);
        }
        
        // Configure the Image to fill from the correct start depending on direction and shape.
        private void ApplyDirectionToImage(SegmentPathVisual vis)
        {
            if (vis.Image == null) return;

            switch (vis.Image.fillMethod)
            {
                case Image.FillMethod.Horizontal:
                    // Start from Left when forward, Right when inverted
                    vis.Image.fillOrigin = vis.Inverted
                        ? (int)Image.OriginHorizontal.Right
                        : (int)Image.OriginHorizontal.Left;
                    vis.Image.fillClockwise = true;
                    break;
                case Image.FillMethod.Vertical:
                    // Start from Bottom when forward, Top when inverted
                    if (vis.Inverted)
                    {
                        vis.Image.fillOrigin = vis.Image.fillOrigin == (int)Image.OriginVertical.Top 
                            ? (int)Image.OriginVertical.Bottom : (int)Image.OriginVertical.Top;
                    }
                    // vis.Image.fillOrigin = vis.Inverted
                    //     ? (int)Image.OriginVertical.Top
                    //     : (int)Image.OriginVertical.Bottom;
                    vis.Image.fillClockwise = true;
                    break;
                case Image.FillMethod.Radial90:
                    // Keep the asset's corner origin; flip clockwise when inverted so it fills from the opposite end.
                    vis.Image.fillOrigin = vis.OriginalFillOrigin;
                    if(vis.Inverted)
                        vis.Image.fillClockwise = vis.Image.fillClockwise ? !vis.OriginalFillClockwise : vis.OriginalFillClockwise;
                    // vis.Image.fillClockwise = vis.Inverted ? !vis.OriginalFillClockwise : vis.OriginalFillClockwise;
                    break;
                default:
                    // Fallback: preserve origin, flip clockwise when inverted
                    // vis.Image.fillOrigin = vis.OriginalFillOrigin;
                    if(vis.Inverted)
                        vis.Image.fillClockwise = vis.Image.fillClockwise ? !vis.Image.fillClockwise : vis.Image.fillClockwise;;
                    // vis.Image.fillClockwise = vis.Inverted ? vis.OriginalFillClockwise : !vis.OriginalFillClockwise;
                    break;
            }
        }
        
        // Load discovered stations from FactSystem into local cache
        private void LoadDiscoveredStationsFromFacts()
        {
            if(FactExists<HashSet<Station_Data>>(RouteManager.Instance.StationFacts, out var discovered))
                discovered = GetFact<HashSet<Station_Data>>(RouteManager.Instance.StationFacts);
            
            _discoveredStationLabels.Clear();
            if(discovered == null) return;

            foreach (var label in discovered.Select(station => $"{station.LinePrefix}{station.Id}"))
                _discoveredStationLabels.Add(label);
        }

        private void RefreshDiscoveredStationsVisibility()
        {
            // if (FactExists<HashSet<Station_Data>>(RouteManager.Instance.StationFacts, out var discovered))
            //     discovered = GetFact<HashSet<Station_Data>>(RouteManager.Instance.StationFacts);
            //
            // if (discovered == null) return;
            //
            // foreach (var label in discovered.Select(station => $"{station.LinePrefix}{station.Id}"))
            // {
            //     var t = _trainStations.FirstOrDefault(x => x.name.Contains(label));
            //     if (t != null) t.gameObject.SetActive(true);
            // }
            
            if(_trainStations == null) return;

            foreach (var label in _discoveredStationLabels)
            {
                var t = _trainStations.FirstOrDefault(x => x.name.Contains(label));
                if (t != null) t.gameObject.SetActive(true);
            }
        }
        
        #endregion
    }
}