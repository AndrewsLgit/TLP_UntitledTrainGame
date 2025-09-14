using System;
using System.Collections.Generic;
using SharedData.Runtime;
using Tools.Runtime;

namespace ServiceInterfaces.Runtime
{
    // Route/Journey service
    public interface IRouteService
    {
        // Domain events (UI/FSM can subscribe)
        event Action<List<Station_Data>> OnJourneyStarted;
        event Action<int, CountdownTimer> OnSegmentStarted;
        event Action<GameTime> OnSegmentEnded;
        event Action<SceneReference> OnDestinationChanged;
        event Action OnJourneyEnded;

        event Action OnPausedRouteRemoved;
        event Action<Station_Data> OnTrainStationDiscovered;
        event Action OnDiscoveredTrainStationsUpdated;

        // Commands
        void StartJourney(TrainRoute_Data route, StationNetwork_Data network);
        void StartJourney(Station_Data start, Station_Data end, StationNetwork_Data network);
        void ResumeJourneyFromPausedStation();
        void StopJourneyEarly();
        void RemovePausedRoute();

        // Queries
        bool HasPendingTrainAtActiveScene();
    }
}