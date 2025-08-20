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
        private AsyncOperation _loadedScene;
        private Scene _sceneToUnload;
        
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
            _sceneName = sceneName;
            // _sceneToUnload = SceneManager.GetActiveScene();
            _sceneToUnload = GetCurrentLevelScene();
            _loadedScene = SceneManager.LoadSceneAsync($"{_scenePath}{_sceneName}", LoadSceneMode.Additive);
            _loadedScene.allowSceneActivation = false;
        }

        public void ActivateScene()
        {
            if (_loadedScene == null)
            {
                Error($"No scene loaded!");
                return;
            }
            _loadedScene.allowSceneActivation = true;

            if (_sceneToUnload.IsValid()) StartCoroutine(UnloadPreviousSceneWhenReady());
            SceneManager.UnloadSceneAsync(_sceneToUnload);
        }
        
        #endregion
        
        #region Utils

        private IEnumerator UnloadPreviousSceneWhenReady()
        {
            // wait for new scene
            yield return _loadedScene;
            
            // Set new scene as active
            Scene newScene = SceneManager.GetSceneByName($"{_sceneName}");
            if (newScene.IsValid())
            {
                SceneManager.SetActiveScene(newScene);
                Info($"Set active scene to: {newScene.name}");
            }
            
            // Unload old scene
            if (_sceneToUnload.IsValid())
            {
                Info($"Unloading previous scene: {_sceneToUnload.name}");
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(_sceneToUnload);
                yield return unloadOperation;
                InfoDone($"Scene unloaded: {_sceneToUnload.name}");
            }
            
            _loadedScene = null;
            _sceneToUnload = default;
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
