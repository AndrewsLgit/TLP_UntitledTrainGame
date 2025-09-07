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
        
        // Movement vector from WASD/Stick
        public event Action<Vector2> OnMove;
        // Interact button pressed
        public event Action OnInteract;
        // Stop train button pressed
        public event Action OnStopTrain; 
        
        #endregion
        
        #endregion
        
        #region Unity API

        private void Start()
        {
            CustomInputManager.Instance.SetPlayerInput(gameObject.GetComponent<PlayerInput>());

        }

        #endregion
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
 
    }
}