using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;
using SceneManager = Manager.Runtime.SceneManager;

namespace Interactable.Runtime
{
    public class TrainStation : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        [SerializeField] private GameTime _timeToInteract;
        private SceneManager _sceneManager;
        [SerializeField] private SceneReference _sceneToLoad;
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        
        public GameTime TimeToInteract => _timeToInteract;
        
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
        #endregion
    }
}