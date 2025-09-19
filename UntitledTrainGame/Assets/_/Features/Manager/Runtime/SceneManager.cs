using System;
using System.Collections;
using Foundation.Runtime;
using ServiceInterfaces.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;


namespace Manager.Runtime
{
    public class SceneManager : FMono, ISceneService
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        // [SerializeField] private string _scenePath = $"_/Levels/";
        [SerializeField] private SceneReference _startScene;
        [SerializeField] private SceneReference _emptyScene;
        [SerializeField] private SceneReference _persistentSceneRef;
        
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
        public string CurrentActiveScene => _preloadedSceneName;
        public SceneReference StartScene => _startScene;
        
        public event Action OnSceneActivated;
        
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
            _persistentScene = UnitySceneManager.GetSceneByName($"{_persistentSceneRef.SceneName}");
            if (!_persistentScene.IsValid()) Error($"Persistent scene '{_persistentSceneRef.SceneName}' not found!");
            else Info($"Persistent scene '{_persistentSceneRef.SceneName}' found!");
            
            // _preloadCoroutine = StartCoroutine(PreloadRoutine(_startScene.SceneName));
            // _replaceCoroutine = StartCoroutine(ReplaceRoutine(_startScene.SceneName)); 
            
        }

        private void Start()
        {
            if (_startScene != null)
            {
                PreloadScene(_startScene);
                ActivateScene();
            }
            
            // ClockManager.Instance.OnLoopEnd += ResetFromStartScene;
        }

        private void OnDestroy()
        {
            // ClockManager.Instance.OnLoopEnd -= ResetFromStartScene;
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
            
            // if (_preloadedSceneName == sceneName)
            // {
            //     Info($"Scene {sceneName} already loaded. Ignoring preload request.");
            //     return;
            // }
            if (_preloadOp != null)
            {
                if (_preloadedSceneName == sceneName)
                {
                    Info($"Scene {sceneName} already loaded. Ignoring preload request.");
                    return;
                }
                StartCoroutine(ReplacePreload(sceneName));
                return;
            }
            
            // Guard: if target is already the currently active non-persistent scene, skip preload
            _currentActiveScene = GetCurrentLevelScene();
            if (IsSameAsCurrent(sceneName))
            {
                Warning($"Requested scene {sceneName} is already active. Skipping preload.");
                _preloadedSceneName = sceneName;
                _preloadOp = null;
                return;
            }
            
            _preloadedSceneName = sceneName;
            
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
            // if nothing is preloaded but target equals current active scene, treat as no-op
            if (_preloadOp == null)
            {
                if (!string.IsNullOrEmpty(_preloadedSceneName) && IsSameAsCurrent(_preloadedSceneName))
                {
                    Warning($"ActivateScene skipped: '{_preloadedSceneName} already active.");
                    return;
                }
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

        public void ResetFromStartScene()
        {
            if (_startScene != null)
            {
                if (_startScene.name == _currentActiveScene.name)
                {
                    Info("Start scene is already active. Unloading then reloading.");
                    UnitySceneManager.UnloadSceneAsync(_currentActiveScene);
                }
                
                // PreloadScene(_emptyScene);
                // ActivateScene();
                    
                PreloadScene(_startScene);
                ActivateScene();
            }
            else Error("Start scene is null!");
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
            
            // If it's actually the same scene (already loaded), don't unload
            if (_currentActiveScene.IsValid() && _currentActiveScene == newScene)
            {
                Warning($"Target scene '{newScene.name}' is already active. Skipping unload.'");
            }
            // Unload old scene if existent and different
            else if (_currentActiveScene.IsValid() && _currentActiveScene != newScene && !IsPersistentScene(_currentActiveScene))
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

            // OnSceneActivated?.Invoke();
        }

        private IEnumerator PreloadRoutine(string sceneName)
        {
            Info($"Starting to preload scene: {sceneName}");
            // If the scene is already loaded, don't load again
            var already = UnitySceneManager.GetSceneByName(sceneName);
            if (already.IsValid() && already.isLoaded && !IsPersistentScene(already))
            {
                Warning($"Scene '{sceneName}' already loaded. Skipping async load.");
                _preloadOp = null;
                _currentActiveScene = GetCurrentLevelScene();
                yield break;
            }
            
            
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
            
            // If next = current, don't preload
            if (IsSameAsCurrent(nextSceneName))
            {
                Warning($"Next scene '{nextSceneName}' is already active. Skipping preload.");
                yield break;
            }
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

        private bool IsSameAsCurrent(string sceneName)
        {
            if(string.IsNullOrEmpty(sceneName)) return false;
            var currentScene = GetCurrentLevelScene();
            return currentScene.IsValid() && currentScene.isLoaded && !IsPersistentScene(currentScene) && currentScene.name == sceneName;
        }
        
        #endregion
    }
}