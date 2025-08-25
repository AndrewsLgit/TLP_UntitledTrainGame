using System.Collections;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;


namespace Tools.Runtime
{
    public class SceneManager : FMono
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        // [SerializeField] private string _scenePath = $"_/Levels/";
        [SerializeField] private SceneReference _startScene;
        
        private string _preloadedSceneName;
        private AsyncOperation _preloadOp;
        
        private Scene _currentActiveScene;
        private Scene _persistentScene;
        private bool _isActivating;
        
        private Coroutine _preloadCoroutine;
        private Coroutine _replaceCoroutine;
        
        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public static SceneManager Instance { get; private set; }
        
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
            
            // Store persistent scene reference
            _persistentScene = gameObject.scene;
        }

        private void Start()
        {
            if (_startScene != null)
            {
                PreloadScene(_startScene);
                ActivateScene();
            }
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

            if (_isActivating)
            {
                Warning("Transition in progress. Ignoring preload request.");
                return;
            }
            
            if (_preloadOp != null)
            {
                if (_preloadedSceneName == sceneName)
                {
                    Info($"Scene {sceneName} already loaded. Ignoring preload request.");
                    return;
                }
                StartCoroutine(ReplacePreload(sceneName));
                // Scene previousLoadedScene = SceneManager.GetSceneByName($"{SceneName}");
                // if (previousLoadedScene.IsValid() && previousLoadedScene.isLoaded)
                // {
                //     SceneManager.UnloadSceneAsync(previousLoadedScene);
                //     Info($"Previous scene unloaded: {previousLoadedScene.name}");
                // }
                // _preloadOp = null;
                return;
            }
            
            _preloadedSceneName = sceneName;
            // _currentActiveScene = SceneManager.GetActiveScene();
            //_currentActiveScene = GetCurrentLevelScene();
            // _preloadOp = SceneManager.LoadSceneAsync($"{_scenePath}{SceneName}", LoadSceneMode.Additive);
            // _preloadOp.allowSceneActivation = false;
            // Info($"Starting to preload scene: {SceneName}");
            
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
            if (_preloadOp == null)
            {
                Error($"No scene preloaded!");
                return;
            }

            if (_isActivating)
            {
                Warning("Already transitioning.");
                return;
            }
            
            _isActivating = true;
            
            // Scene preloadedScene = SceneManager.GetSceneByName($"{SceneName}");
            // if (!preloadedScene.IsValid() || !preloadedScene.isLoaded)
            // {
            //     Error($"Scene not loaded!");
            //     _preloadOp = null;
            //     return;
            // }
            //
            // if (_currentActiveScene.IsValid()) StartCoroutine(UnloadPreviousSceneWhenReady());
            // // SceneManager.UnloadSceneAsync(_currentActiveScene);
            _preloadOp.allowSceneActivation = true;
            
            StartCoroutine(UnloadPreviousSceneWhenReady());
 
        }
        
        #endregion
        
        #region Utils

        private IEnumerator UnloadPreviousSceneWhenReady()
        {
            // wait for new scene
            yield return _preloadOp;
            
            // Set new scene as active
            Scene newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName($"{_preloadedSceneName}");
            if (newScene.IsValid())
            {
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(newScene);
                // _currentActiveScene = newScene;
                Info($"Set active scene to: {newScene.name}");
            }
            
            // Unload old scene if existent and different
            if (_currentActiveScene.IsValid() && _currentActiveScene != newScene && !IsPersistentScene(_currentActiveScene))
            {
                Info($"Unloading previous scene: {_currentActiveScene.name}");
                AsyncOperation unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_currentActiveScene);
                // _preloadOp.allowSceneActivation = true;
                if (unloadOperation != null) yield return unloadOperation;
                InfoDone($"Scene unloaded: {_currentActiveScene.name}");
            }
            else if (IsPersistentScene(_currentActiveScene)) Info($"Skipping unload of persistent scene: {_currentActiveScene.name}");
            
            _preloadOp = null;
            _currentActiveScene = newScene;
            _isActivating = false;
            
        }

        private IEnumerator PreloadRoutine(string sceneName)
        {
            Info($"Starting to preload scene: {sceneName}");
            _preloadOp = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync($"{sceneName}", LoadSceneMode.Additive);
            if (_preloadOp == null)
            {
                Error($"Failed to start loading scene '{sceneName}'. Check Build Settings path.");
                yield break;
            }
            
            _preloadOp.allowSceneActivation = false;
            
            // park at 90%
            while(_preloadOp.progress < 0.9f)
                yield return null;
            Info($"Preload ready at 90% for: {sceneName}");
        }

        private IEnumerator ReplacePreload(string nextSceneName)
        {
            Info($"Replacing preloaded '{_preloadedSceneName}' with '{nextSceneName}'");
            
            yield return FinishAndUnload(_preloadedSceneName, _preloadOp);

            _preloadOp = null;
            _preloadedSceneName = nextSceneName;

            _currentActiveScene = GetCurrentLevelScene();
            yield return PreloadRoutine(nextSceneName);
        }
        
        private IEnumerator FinishAndUnload(string sceneName, AsyncOperation asyncOp)
        {
            if (asyncOp == null) yield break;
            asyncOp.allowSceneActivation = true;
            yield return asyncOp;
            
            //todo: check with setactive scene
            var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
            if (s.IsValid() && s.isLoaded && !IsPersistentScene(s))
            {
                var unloadOp = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(s);
                if (unloadOp != null) yield return unloadOp;
                Info($"Scene unloaded: {s.name}");
            }
            else if (IsPersistentScene(s)) Info($"Skipping unload of persistent scene: {s.name}");
            // while (!asyncOp.isDone) yield return null;
            // SceneManager.UnloadSceneAsync(SceneName);
        }

        private Scene GetCurrentLevelScene()
        {

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if(scene.IsValid() && scene.isLoaded && !IsPersistentScene(scene)) return scene;
            }

            return UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        }

        private bool IsPersistentScene(Scene scene)
        {
            return scene.IsValid() && scene == _persistentScene;
        }
        
        #endregion
    }
}