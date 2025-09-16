using Foundation.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using SharedData.Runtime.Events;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class Bench : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        private IClockService _clockManager;
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
            _clockManager = ServiceRegistry.Resolve<IClockService>();

        }
        #endregion

        #region Main Methods
        
        public void Interact()
        {
            
            GetClockManager();
            
            Wait();
        }

        public void Sleep()
        {
            GetClockManager();
            Info($"Bench sleep selected");
            // Add clock manager sleep event (reset loop)
            // _clockManager.JumpToNextEventWithTag("Sleep");
            _clockManager.SleepToLoopEnd();
        }

        public void Wait()
        {
            GetClockManager();
            Info($"Bench wait selected");
            
            TimeEvent foundEvent = null;
            Info("Interacting with Bench");
            
            foundEvent = _clockManager.JumpToNextEventWithTag("Train");

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
        
        #region Helpers/Utils

        private void GetClockManager()
        {
            if (_clockManager == null)
                _clockManager = ServiceRegistry.Resolve<IClockService>();
            Assert.IsNotNull(_clockManager, "ClockManager not found!");
        }
        
        #endregion
    }
}