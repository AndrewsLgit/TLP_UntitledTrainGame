using System;
using Foundation.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = Tools.Runtime.SceneManager;

namespace Interactable.Runtime
{
    public class TrainStation : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        private float _timeToInteract;
        private SceneManager _sceneManager;
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