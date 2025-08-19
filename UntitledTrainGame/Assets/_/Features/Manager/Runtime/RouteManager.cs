using System.Collections.Generic;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Manager.Runtime
{
    public class RouteManager : FMono
    {
        #region Variables

        #region Private
        // Private Variables
        
        [SerializeField] private StationNetwork_Data _stationNetwork;
        [SerializeField] private TrainRoute_Data _testTrainRoute;

        private List<Station_Data> _segments = new List<Station_Data>();
        private int _currentStationIndex = 0;

        // Private Variables
        #endregion

        #endregion
        
        #region Unity API
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _currentStationIndex = 0;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
        #endregion
        
        #region Main Methods

        [ContextMenu("Start Test Journey")]
        private void StartJourney()
        {
            _currentStationIndex = 0;
            StartJourney(_testTrainRoute);
        }
        // calculate segments and start traveling through them
        private void StartJourney(TrainRoute_Data trainRoute)
        {
            // get all stations from route.start to route.end from StationGraphSO
            _segments = _stationNetwork.CalculatePath(trainRoute.StartStation, trainRoute.EndStation);
            if (_segments == null)
            {
                Error($"Path not calculated!");
                return;
            }
            Info($"Starting journey from {_segments[0].DisplayName} to {_segments[^1].DisplayName}");
            StartSegment(_currentStationIndex);
        }

        private void StartSegment(int index)
        {
            Info($"Starting segment {index} of {_segments.Count-1}");
            if (index >= _segments.Count-1)
            {
                EndJourney();
                return;
            }
            
            Info($"Segment {_segments[index].DisplayName} -> {_segments[index + 1].DisplayName}");
            EndSegment();
        }
        
        private void EndSegment()
        {
            _currentStationIndex++;
            StartSegment(_currentStationIndex);
        }

        private void EndJourney()
        {
            InfoDone($"Journey ended.");
        }
        
        #endregion
    }
}
