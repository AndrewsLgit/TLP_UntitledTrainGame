using System.Collections.Generic;
using SharedData.Runtime;
using Tools.Runtime;

namespace ToServiceInterfacesols.Runtime
{
    // UI service (temporary façade while we split UIManager)
    public interface IUiService
    {
        // Map and travel progress
        void ShowMap();
        void HideMap();
        void CreateProgressBarsForRoute(List<Station_Data> segments);
        void StartMapSegmentProgress(int segmentIndex, CountdownTimer timer);
        void ResetTravelUiState();

        // Game menus (optional – keep what you need)
        void PauseGame();
        void ResumeGame();
        void GameOver();
    }

}