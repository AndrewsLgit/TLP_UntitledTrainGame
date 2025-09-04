using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using SharedData.Runtime.Events;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class Bench : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        private ClockManager _clockManager;
        private GameTime _timeToInteract;
        
        #endregion
        
        #region Public
        
        public GameTime TimeToInteract => _timeToInteract;
        public InteractionType InteractionType => InteractionType.Bench;

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
            
            if (_clockManager == null)
                _clockManager = ClockManager.Instance;
            Assert.IsNotNull(_clockManager, "ClockManager not found!");
            
            TimeEvent foundEvent = null;
            Info("Interacting with Bench");
            
            foundEvent = _clockManager.FindNextEventWithTag("Train");
            // else
            // Error("ClockManager not found!");

            if (foundEvent != null)
            {
                Info($"Event found: {foundEvent}. Advancing time to {foundEvent.m_Start}");
            }
            else Warning("No event found!");
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}