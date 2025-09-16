using System;
using Foundation.Runtime;
using Player.Runtime;
using ServiceInterfaces.Runtime;
using Services.Runtime;
using SharedData.Runtime;
using Tools.Runtime;
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
        
        private IClockService _clockService;
        private bool _isNextEventNull;

        // State
        private IBenchChoiceUI.BenchChoiceModel _model;
        // private int _selectedIndex = 0;        // 0 = Sleep, 1 = Wait
        // private bool _isOpen = false;
        //
        // // Small cooldown to avoid rapid repeats when holding the nav key/stick
        // private float _navRepeatCooldown = 0.18f;
        // private float _navRepeatTimer = 0f;
        
        private readonly ChoiceSelectionController _selector = new ChoiceSelectionController(0.18f);

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
            
            _selector.OnSelectionChanged += _ => UpdateVisuals();
            _selector.OnSubmit += index => OnChoiceSelected?.Invoke(index);
            _selector.OnCancel += () => OnCancelled?.Invoke();
        }

        private void Start()
        {
            _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            _clockService = ServiceRegistry.Resolve<IClockService>();
            _isNextEventNull = _clockService.GetNextEvent() == null;
        }

        private void OnDisable()
        {
            // Safety: unsub if disabled unexpectedly
            UnsubscribeInput();
        }

        private void Update()
        {
            _selector.Tick(Time.deltaTime);
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
            
            // Find the router if not assigned
            if (_inputRouter == null)
                _inputRouter = FindAnyObjectByType<PlayerInputRouter>();
            
            
            
            // Subscribe to UI action map events: Navigate/Submit/Cancel
            SubscribeInput();
            
            // Show the panel
            if(_rootPanel != null)
                _rootPanel.SetActive(true);
            
            _isNextEventNull = _clockService.GetNextEvent() == null;

            if (!_isNextEventNull)
            {
                _selector.Open(_model.Options.Length);
                // Info($"Found event {_clockService.GetNextEvent().m_EventName}");

                // UpdateVisuals();
                // Start with all options Unselected
                _sleepSelected.SetActive(false);
                _waitSelected.SetActive(false);
                _sleepUnselected.SetActive(true);
                _waitUnselected.SetActive(true);
            }
            else
            {
                _selector.Open(0);
                
                // UpdateVisuals();
                // Start with all options Unselected
                _sleepSelected.SetActive(true);
                _waitSelected.SetActive(false);
                _sleepUnselected.SetActive(false);
                _waitUnselected.SetActive(false);
            }
            
        }

        // Close is called by PlayerInteraction after selection/cancel.
        public void Close()
        {
            // Hide panel
            if(_rootPanel != null)
                _rootPanel.SetActive(false);
            
            // Reset state
            // _isOpen = false;
            _model = null;
            
            _selector.Close();
            
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
            if (!_selector.IsOpen) return;
            
            _selector.HandleNavigate(direction);
            UpdateVisuals();
        }
        
        // Submit handler: confirm selection (0 = Sleep, 1 = Wait)
        private void HandleUISubmit()
        {
            if (!_selector.IsOpen) return;
            // if (_selector.SelectedIndex < 0) return;
            
            // Notify PlayerInteraction of selection (Close() and switch action maps)
            _selector.Submit();
        }
        
        // Cancel handler: dismiss the UI without selecting
        private void HandleUICancel()
        {
            if (!_selector.IsOpen) return;
            
            // Notify PlayerInteraction of cancel (Close() and switch action maps)
            _selector.Cancel();
        }
        
        #endregion
        
        #region Visuals

        private void UpdateVisuals()
        {
            
            int selectedIndex = _selector.SelectedIndex;
            // if(selectedIndex < 0) return;
            
            Info($"Selected index: {selectedIndex}");
            bool sleepSelected = selectedIndex == 0;
            bool waitSelected = selectedIndex == 1;

            if (!_isNextEventNull)
            {
                if(_sleepSelected != null) _sleepSelected.SetActive(sleepSelected);
                if(_sleepUnselected != null) _sleepUnselected.SetActive(!sleepSelected);
            
                if(_waitSelected != null) _waitSelected.SetActive(waitSelected);
                if(_waitUnselected != null) _waitUnselected.SetActive(!waitSelected); 
            }
            else
            {
                if(_sleepSelected != null) _sleepSelected.SetActive(sleepSelected);
                if(_sleepUnselected != null) _sleepUnselected.SetActive(!sleepSelected);
            
                if(_waitSelected != null) _waitSelected.SetActive(false);
                if(_waitUnselected != null) _waitUnselected.SetActive(false); 
            }
            
        }
        #endregion
    }
}