using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using UnityEngine;
using SceneManager = Manager.Runtime.SceneManager;

namespace Interactable.Runtime
{
    public class StationBuilding : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables

        private SceneManager _sceneManager;
        [SerializeField] private SceneReference _sceneToLoad;
        [SerializeField] private bool _isFromInsideToOutside = false;
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        
        public GameTime TimeToInteract { get; }
        public InteractionType InteractionType => InteractionType.EnterBuilding;

        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _sceneManager = SceneManager.Instance;
            //todo: replace this with a proper event system
            //SetFact("isPending", false, false);
            //RouteManager.Instance.RemovePausedRoute();
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            Info($"Interacting with Train Station");
            // SceneManager.LoadScene(_sceneToLoad);
            RouteManager.Instance.RemovePausedRoute();
            _sceneManager.PreloadScene(_sceneToLoad);
            _sceneManager.ActivateScene();
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }
        #endregion
    }
}