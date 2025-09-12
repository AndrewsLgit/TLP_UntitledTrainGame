using Foundation.Runtime;
using SharedData.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    public class PlayerWorldPopupPresenter : FMono, IInteractionPopupPresenter
    {
        #region Variables

        #region Private

        // --- Start of Private Variables ---

        [Header("UI References - Interaction Bubble")]
        [SerializeField] private GameObject _interactionBubble;
        [SerializeField] private GameObject _interactionBubbleEnter;
        [SerializeField] private GameObject _interactionBubbleTrain;
        [SerializeField] private GameObject _interactionBubbleBench;
        [SerializeField] private GameObject _interactionBubblePickUp;
        [SerializeField] private GameObject _interactionBubbleDialog;


        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---


        // --- End of Public Variables --- 

        #endregion

        #endregion

        #region Unity API

        private void Awake()
        {
            Hide();
        }
        
        #endregion

        #region Main Methods
        
        public void Show(InteractionType type)
        {
            // Defensive: if any are missing, just return to avoid NREs in early setup
            if (_interactionBubble == null) return;

            // Hide all specific icons first
            SetActiveSafe(_interactionBubbleEnter, false);
            SetActiveSafe(_interactionBubbleTrain, false);
            SetActiveSafe(_interactionBubbleBench, false);
            SetActiveSafe(_interactionBubblePickUp, false);
            SetActiveSafe(_interactionBubbleDialog, false);

            // Show the common bubble and the specific icon
            _interactionBubble.SetActive(true);

            switch (type)
            {
                case InteractionType.Train:
                    SetActiveSafe(_interactionBubbleTrain, true);
                    break;
                case InteractionType.Dialog:
                case InteractionType.Inspect:
                case InteractionType.Read:
                    SetActiveSafe(_interactionBubbleDialog, true);
                    break;
                case InteractionType.Bench:
                    SetActiveSafe(_interactionBubbleBench, true);
                    break;
                case InteractionType.EnterBuilding:
                    SetActiveSafe(_interactionBubbleEnter, true);
                    break;
                case InteractionType.PickUp:
                    SetActiveSafe(_interactionBubblePickUp, true);
                    break;
                default:
                    // If new types appear, fallback to just showing the common bubble
                    break;
            }

        }

        public void Hide()
        {
            SetActiveSafe(_interactionBubbleEnter, false);
            SetActiveSafe(_interactionBubbleTrain, false);
            SetActiveSafe(_interactionBubbleBench, false);
            SetActiveSafe(_interactionBubblePickUp, false);
            SetActiveSafe(_interactionBubbleDialog, false);
            SetActiveSafe(_interactionBubble, false);

        }
        
        #endregion

        #region Helpers/Utils

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        #endregion
    }
}