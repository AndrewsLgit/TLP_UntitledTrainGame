using System;
using Foundation.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interactable.Runtime
{
    public class TrainStation : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        private float _timeToInteract;
        private SceneLoader _sceneLoader;
        [SerializeField] private string _sceneToLoad;
        
        // Private Variables
        #endregion

        #region Public
        // Public Variables
        
        public float TimeToInteract => _timeToInteract;
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            _sceneLoader = SceneLoader.Instance;
        }

        #endregion
        
        #region Main Methods
        public void Interact()
        {
            Info($"Interacting with Train Station");
            // SceneManager.LoadScene(_sceneToLoad);
            _sceneLoader.PreloadScene(_sceneToLoad);
            _sceneLoader.ActivateScene();
        }
        #endregion
    }
}