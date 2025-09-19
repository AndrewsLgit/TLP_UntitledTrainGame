using DialogSystem.Runtime;
using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Interactable.Runtime
{

    public class Dialog : FMono, IInteractable 
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        [Header("Dialog to start when interacting")]
        [SerializeField] private DialogNode _dialogStart;

        private DialogController _dialogController;
        // --- End of Private Variables --- 
        #endregion

        #region Public
        // --- Start of Public Variables ---
        public GameTime TimeToInteract { get; }

        public InteractionType InteractionType { get; } = InteractionType.Dialog;
        // --- End of Public Variables --- 
        #endregion

        #endregion

        #region Unity API
    
        private void Awake() { }

        private void Start()
        {
            _dialogController = DialogController.Instance;
        }

        private void Update() { }

        private void FixedUpdate() { }

        private void OnEnable() { }

        private void OnDisable() { }

        private void OnDestroy() { }

        #endregion

        #region Main Methods
        public void Interact()
        {
            _dialogController.StartConversation(_dialogStart, _dialogStart.Character.Id);
        }

        public void AdvanceTime(GameTime time)
        {
            throw new System.NotImplementedException();
        }
        #endregion

        #region Helpers/Utils
        #endregion
    }
}
