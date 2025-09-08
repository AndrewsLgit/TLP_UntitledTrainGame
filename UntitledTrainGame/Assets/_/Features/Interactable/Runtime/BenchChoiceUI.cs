using System;
using Foundation.Runtime;
using Player.Runtime;
using SharedData.Runtime;
using UnityEngine;
using UnityEngine.Assertions;

namespace Interactable.Runtime
{
    public class BenchChoiceUI : FMono, IBenchChoiceUI
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References to draw simple selected/unselected visuals for two options.
        // You can wire these GameObjects in the inspector to show a highlight/arrow/checkbox, etc.
        [Header("UI Elements")]
        [SerializeField] private GameObject _rootPanel;       // Entire bench choice panel (enabled/disabled here)
        [SerializeField] private GameObject _sleepUnselected;
        [SerializeField] private GameObject _sleepSelected;
        [SerializeField] private GameObject _waitUnselected;
        [SerializeField] private GameObject _waitSelected;
        
        // Router providing UI action map events: Navigate/Submit/Cancel.
        // If not set, we’ll find one in the scene at runtime.
        [Header("Input")]
        [SerializeField] private PlayerInputRouter _inputRouter;

        // State
        private IBenchChoiceUI.BenchChoiceModel _model;
        private int _selectedIndex = 0;        // 0 = Sleep, 1 = Wait
        private bool _isOpen = false;

        // Small cooldown to avoid rapid repeats when holding the nav key/stick
        private float _navRepeatCooldown = 0.18f;
        private float _navRepeatTimer = 0f;

        // End of Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        public event Action<int> OnChoiceSelected;
        public event Action OnCancelled;
        
        // End of Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            if(_rootPanel != null) _rootPanel.SetActive(false);
        }

        private void Start()
        {
            _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
        }

        private void OnDisable()
        {
            // Safety: unsub if disabled unexpectedly
            UnsubscribeInput();
        }

        private void Update()
        {
            // Cooldown tick for repeating nav key/stick
            if(_navRepeatTimer > 0f)
                _navRepeatTimer -= Time.deltaTime;
        }

        #endregion
        
        #region Interface Implementation
        
        // Open is called by PlayerInteraction after switching to UI action map.
        public void Open(IBenchChoiceUI.BenchChoiceModel model)
        {
            // Cache/validate model
            _model = model ?? new IBenchChoiceUI.BenchChoiceModel()
            {
                Options = new[] { "Sleep, Wait" },
                Targets = null
            };
            if (_model.Options == null || _model.Options.Length < 2)
                _model.Options = new[] { "Sleep, Wait" };
            
            // Set initial state
            _selectedIndex = 0;
            _isOpen = true;
            _navRepeatTimer = 0f;
            
            // Find the router if not assigned
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            
            // Subscribe to UI action map events: Navigate/Submit/Cancel
            SubscribeInput();
            
            // Show the panel
            if(_rootPanel != null)
                _rootPanel.SetActive(true);

            UpdateVisuals();
        }

        // Close is called by PlayerInteraction after selection/cancel.
        public void Close()
        {
            // Hide panel
            if(_rootPanel != null)
                _rootPanel.SetActive(false);
            
            // Reset state
            _isOpen = false;
            _model = null;
            
            // Unsubscribe from UI action map events (PlayerInteraction will switch action map)
            UnsubscribeInput();
        }

        
        #endregion
        
        #region Input Wiring
        
        private void SubscribeInput()
        {
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            
            Assert.IsNotNull(_inputRouter, "PlayerInputRouter not found!");

            // These are UI action map events in PlayerInputRouter:
            // - OnUINavigate(Vector2)
            // - OnUISubmit()
            // - OnUICancel()
            _inputRouter.OnUINavigate += HandleUINavigate;
            _inputRouter.OnUISubmit += HandleUISubmit;
            _inputRouter.OnUICancel += HandleUICancel;
        }

        private void UnsubscribeInput()
        {
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            
            Assert.IsNotNull(_inputRouter, "PlayerInputRouter not found!");
            
            _inputRouter.OnUINavigate -= HandleUINavigate;
            _inputRouter.OnUISubmit -= HandleUISubmit;
            _inputRouter.OnUICancel -= HandleUICancel;
        }
        
        #endregion
        
        #region Handlers
        
        // Navigate handler: we only care about vertical axis (W/S, Up/Down, DPad Y, etc.)
        private void HandleUINavigate(Vector2 direction)
        {
            if (!_isOpen) return;
            
            // Ignore tiny input/noise
            if(Mathf.Abs(direction.y) < 0.05f) return;
            
            // Simple repeat gating so holding down doesn't spam every frame
            if (_navRepeatTimer > 0f) return;
            _navRepeatTimer = _navRepeatCooldown;
            
            // Toggle selection index between 0 and 1
            _selectedIndex = (_selectedIndex == 0) ? 1 : 0;
            UpdateVisuals();
        }
        
        // Submit handler: confirm selection (0 = Sleep, 1 = Wait)
        private void HandleUISubmit()
        {
            if (!_isOpen) return;
            
            // Notify PlayerInteraction of selection (Close() and switch action maps)
            OnChoiceSelected?.Invoke(_selectedIndex);
        }
        
        // Cancel handler: dismiss the UI without selecting
        private void HandleUICancel()
        {
            if (!_isOpen) return;
            
            // Notify PlayerInteraction of cancel (Close() and switch action maps)
            OnCancelled?.Invoke();
        }
        
        #endregion
        
        #region Visuals

        private void UpdateVisuals()
        {
            bool sleepSelected = _selectedIndex == 0;
            bool waitSelected = _selectedIndex == 1;
            
            if(_sleepSelected != null) _sleepSelected.SetActive(sleepSelected);
            if(_sleepUnselected != null) _sleepUnselected.SetActive(!sleepSelected);
            
            if(_waitSelected != null) _waitSelected.SetActive(waitSelected);
            if(_waitUnselected != null) _waitUnselected.SetActive(!waitSelected);
        }
        #endregion
    }
}