using System.Collections;
using Foundation.Runtime;
using SharedData.Runtime;
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
        private bool _isTransitioning;
        
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
            if (string.IsNullOrEmpty(sceneName))
            {
                Error("Scene name is null or empty!");
                return;
            }

            if (_isTransitioning)
            {
                Warning("Transition in progress. Ignoring preload request.");
                return;
            }
            
            if (_preloadedScene != null)
            {
                if (_sceneName == sceneName)
                {
                    Info($"Scene {sceneName} already loaded. Ignoring preload request.");
                    return;
                }
                StartCoroutine(ReplacePreload(sceneName));
                // Scene previousLoadedScene = SceneManager.GetSceneByName($"{_sceneName}");
                // if (previousLoadedScene.IsValid() && previousLoadedScene.isLoaded)
                // {
                //     SceneManager.UnloadSceneAsync(previousLoadedScene);
                //     Info($"Previous scene unloaded: {previousLoadedScene.name}");
                // }
                // _preloadedScene = null;
            }
            
            _sceneName = sceneName;
            // _currentActiveScene = SceneManager.GetActiveScene();
            _currentActiveScene = GetCurrentLevelScene();
            // _preloadedScene = SceneManager.LoadSceneAsync($"{_scenePath}{_sceneName}", LoadSceneMode.Additive);
            // _preloadedScene.allowSceneActivation = false;
            // Info($"Starting to preload scene: {_sceneName}");
            
            StartCoroutine(PreloadRoutine(sceneName));
        }

        public void PreloadScene(SceneReference sceneRef)
        {
            if (sceneRef == null)
            {
                Error("SceneReference is null");
                return;
            }
            PreloadScene(sceneRef.SceneName);
        }

        public void ActivateScene()
        {
            if (_preloadedScene == null)
            {
                Error($"No scene preloaded!");
                return;
            }

            if (_isTransitioning)
            {
                Warning("Already transitioning.");
                return;
            }
            
            _isTransitioning = true;
            
            // Scene preloadedScene = SceneManager.GetSceneByName($"{_sceneName}");
            // if (!preloadedScene.IsValid() || !preloadedScene.isLoaded)
            // {
            //     Error($"Scene not loaded!");
            //     _preloadedScene = null;
            //     return;
            // }
            //
            // if (_currentActiveScene.IsValid()) StartCoroutine(UnloadPreviousSceneWhenReady());
            // // SceneManager.UnloadSceneAsync(_currentActiveScene);
            _preloadedScene.allowSceneActivation = true;
            
            StartCoroutine(UnloadPreviousSceneWhenReady());
 
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
            
            // Unload old scene if existent and different
            if (_currentActiveScene.IsValid() && _currentActiveScene != newScene)
            {
                Info($"Unloading previous scene: {_currentActiveScene.name}");
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(_currentActiveScene);
                // _preloadedScene.allowSceneActivation = true;
                if (unloadOperation != null) yield return unloadOperation;
                InfoDone($"Scene unloaded: {_currentActiveScene.name}");
            }
            
            _preloadedScene = null;
            _currentActiveScene = default;
            _isTransitioning = false;
        }

        private IEnumerator PreloadRoutine(string sceneName)
        {
            Info($"Starting to preload scene: {sceneName}");
            _preloadedScene = SceneManager.LoadSceneAsync($"{_scenePath}{sceneName}", LoadSceneMode.Additive);
            if (_preloadedScene == null)
            {
                Error($"Failed to start loading scene '{sceneName}'. Check Build Settings path.");
                yield break;
            }
            
            _preloadedScene.allowSceneActivation = false;
            
            // park at 90%
            while(_preloadedScene.progress < 0.9f)
                yield return null;
            Info($"Preload ready at 90% for: {sceneName}");
        }

        private IEnumerator ReplacePreload(string nextSceneName)
        {
            Info($"Replacing preloaded '{_sceneName}' with '{nextSceneName}'");
            
            yield return FinishAndUnload(_sceneName, _preloadedScene);

            _preloadedScene = null;
            _sceneName = nextSceneName;

            _currentActiveScene = GetCurrentLevelScene();
            yield return PreloadRoutine(nextSceneName);
        }
        
        private IEnumerator FinishAndUnload(string sceneName, AsyncOperation asyncOp)
        {
            if (asyncOp == null) yield break;
            asyncOp.allowSceneActivation = true;
            yield return asyncOp;
            
            var s = SceneManager.GetSceneByName(sceneName);
            if (s.IsValid() && s.isLoaded)
            {
                var unloadOp = SceneManager.UnloadSceneAsync(s);
                if (unloadOp != null) yield return unloadOp;
                Info($"Scene unloaded: {s.name}");
            }
            // while (!asyncOp.isDone) yield return null;
            // SceneManager.UnloadSceneAsync(sceneName);
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
