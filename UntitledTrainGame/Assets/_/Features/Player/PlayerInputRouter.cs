using System;
using Foundation.Runtime;
using Manager.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    [RequireComponent(typeof(Input))]
    public class PlayerInputRouter : FMono
    {
        #region Variables
        
        #region Private
        // Private Variables
        
        [SerializeField] private EmptyEventChannel _onPlayerJourneyEnd;
        #endregion
        
        #region Public
        
        // --- Player Action Map events ---
        // Movement vector from WASD/Stick
        public event Action<Vector2> OnMove;
        // Interact button pressed
        public event Action OnInteract;
        // Stop train button pressed
        public event Action OnStopTrain; 
        
        // --- UI Action Map events ---
        // Fired with UI/Navigate (e.g., WASD/arrow keys or dpad/left stick when on UI map)
        public event Action<Vector2> OnUINavigate;
        // Fired with UI/Submit (e.g., Enter/A button)
        public event Action OnUISubmit;
        // Fired with UI/Cancel (e.g., Escape/B button)
        public event Action OnUICancel;
        
        
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            CustomInputManager.Instance.SetPlayerInput(gameObject.GetComponent<PlayerInput>());

        }

        #endregion
        
        // --- Player Action Map handlers (bind these in the PlayerInput component to the "Player" action map) ---
        // Wire these up from Unity InputSystem via PlayerInput component
        // Example: Player/Move -> calls Move(context)
        public void Move(InputAction.CallbackContext context)
        {
            Info($"PlayerInputRouter.Move: {context}");
            if (context is { performed: false, canceled: false }) return;
            var value = context.ReadValue<Vector2>();
            if (context.canceled) value = Vector2.zero;
            OnMove?.Invoke(value);
        }

        // Example: Player/Interact -> calls Interact(context)
        public void Interact(InputAction.CallbackContext context)
        {
            Info($"PlayerInputRouter.Interact: {context}");
            // if (!context.performed) return;
            if (context.phase != InputActionPhase.Canceled) return;
            OnInteract?.Invoke();
        }

        // Example: Player/StopTrain -> calls StopTrain(context)
        public void StopTrain(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Canceled) return;
            OnStopTrain?.Invoke();
            _onPlayerJourneyEnd?.Invoke();
        }
        
        // --- UI Action Map handlers (bind these in the PlayerInput component to the "UI" action map) ---

        // Example binding: UI/Navigate -> calls UINavigate(context)
        public void UINavigate(InputAction.CallbackContext context)
        {
            // We forward performed/canceled to allow "release" to be handled if needed,
            // but typical UI nav only needs performed.
            if (!context.performed && !context.canceled) return;
            var value = context.ReadValue<Vector2>();
            // Maybe remove next line when navigating dialogs + map elements
            if (context.canceled) value = Vector2.zero;
            OnUINavigate?.Invoke(value);
        }

        // Example binding: UI/Submit -> calls UISubmit(context)
        public void UISubmit(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed) return;
            OnUISubmit?.Invoke();
        }

        // Example binding: UI/Cancel -> calls UICancel(context)
        public void UICancel(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Performed) return;
            OnUICancel?.Invoke();
        }

 
    }
}