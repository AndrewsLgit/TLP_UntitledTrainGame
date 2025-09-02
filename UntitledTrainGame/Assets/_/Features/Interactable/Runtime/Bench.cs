using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using SharedData.Runtime.Events;

namespace Interactable.Runtime
{
    public class Bench : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        private ClockManager _clockManager;
        
        #endregion
        
        #region Public
        
        public GameTime TimeToInteract { get; }
        
        #endregion
        
        #endregion
        
        #region Unity API
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
            _clockManager = ClockManager.Instance;
            
        }
        #endregion

        #region Main Methods
        
        public void Interact()
        {
            TimeEvent foundEvent = null;
            Info("Interacting with Bench");
            if (_clockManager != null)
               foundEvent = _clockManager.FindNextEventWithTag("Train");
            else
                Error("ClockManager not found!");

            if (foundEvent != null)
            {
                Info($"Event found: {foundEvent}. Advancing time to {foundEvent.m_Start}");
            }
            else Warning("No event found!");
        }

        #endregion
    }
}