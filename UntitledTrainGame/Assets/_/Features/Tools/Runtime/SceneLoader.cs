using System.Collections;
using Foundation.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tools.Runtime
{
    public class SceneLoader : FMono
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        [SerializeField] private string _scenePath = $"_/Levels/";
        private string _sceneName;
        private AsyncOperation _preloadedScene;
        private Scene _currentActiveScene;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public static SceneLoader Instance { get; private set; }
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            // Find instance of this class, if existent -> destroy that instance
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                Error("There is already an instance of this class! Destroying this one!");
                return;
            }

            // Assign instance as this current object
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #endregion
        
        #region Main Methods

        public void PreloadScene(string sceneName)
        {
            if (_preloadedScene != null)
            {
                Scene previousLoadedScene = SceneManager.GetSceneByName($"{_sceneName}");
                if (previousLoadedScene.IsValid() && previousLoadedScene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(previousLoadedScene);
                    Info($"Previous scene unloaded: {previousLoadedScene.name}");
                }
                _preloadedScene = null;
            }
            
            _sceneName = sceneName;
            // _currentActiveScene = SceneManager.GetActiveScene();
            _currentActiveScene = GetCurrentLevelScene();
            _preloadedScene = SceneManager.LoadSceneAsync($"{_scenePath}{_sceneName}", LoadSceneMode.Additive);
            _preloadedScene.allowSceneActivation = false;
            Info($"Starting to preload scene: {_sceneName}");
        }

        public void ActivateScene()
        {
            if (_preloadedScene == null)
            {
                Error($"No scene loaded!");
                return;
            }
            
            Scene preloadedScene = SceneManager.GetSceneByName($"{_sceneName}");
            if (!preloadedScene.IsValid() || !preloadedScene.isLoaded)
            {
                Error($"Scene not loaded!");
                _preloadedScene = null;
                return;           
            }

            if (_currentActiveScene.IsValid()) StartCoroutine(UnloadPreviousSceneWhenReady());
            // SceneManager.UnloadSceneAsync(_currentActiveScene);
            _preloadedScene.allowSceneActivation = true;
 
        }
        
        #endregion
        
        #region Utils

        private IEnumerator UnloadPreviousSceneWhenReady()
        {
            // wait for new scene
            yield return _preloadedScene;
            
            // Set new scene as active
            Scene newScene = SceneManager.GetSceneByName($"{_sceneName}");
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                Info($"Set active scene to: {newScene.name}");
            }
            
            // Unload old scene
            if (_currentActiveScene.IsValid())
            {
                Info($"Unloading previous scene: {_currentActiveScene.name}");
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(_currentActiveScene);
                // _preloadedScene.allowSceneActivation = true;
                yield return unloadOperation;
                InfoDone($"Scene unloaded: {_currentActiveScene.name}");
            }
            
            _preloadedScene = null;
            _currentActiveScene = default;
        }

        private Scene GetCurrentLevelScene()
        {
            Scene persistentScene = gameObject.scene;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if(scene.IsValid() && scene.isLoaded) return scene;
            }

            return SceneManager.GetActiveScene();
        }
        
        #endregion
    }
}
