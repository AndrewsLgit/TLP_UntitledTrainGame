using Foundation.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using UnityEngine;
// using SceneManager = Manager.Runtime.SceneManager;

namespace Interactable.Runtime
{
    public class StationBuilding : FMono, IInteractable
    {
        #region Variables
        
        #region Private
        // Private Variables

        private ISceneService _sceneManager;
        private IRouteService _routeManager;
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
            _sceneManager = ServiceRegistry.Resolve<ISceneService>();
            _routeManager = ServiceRegistry.Resolve<IRouteService>();
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
            // RouteManager.Instance.RemovePausedRoute();
            _routeManager.RemovePausedRoute();
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