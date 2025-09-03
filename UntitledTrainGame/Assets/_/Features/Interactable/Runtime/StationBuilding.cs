using Foundation.Runtime;
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

        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _sceneManager = SceneManager.Instance;
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            Info($"Interacting with Train Station");
            // SceneManager.LoadScene(_sceneToLoad);
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