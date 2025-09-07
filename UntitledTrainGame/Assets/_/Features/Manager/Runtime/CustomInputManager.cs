using Foundation.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Manager.Runtime
{
    public class CustomInputManager : FMono
    {
        [SerializeField] private PlayerInput _playerInput;

        private string _cachedScheme;
        private InputDevice[] _cachedDevices;
        
        public static CustomInputManager Instance { get; private set; }

        #region Unity API
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            CacheSchemeAndDevices();
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            _playerInput = FindAnyObjectByType<PlayerInput>();
            if (_playerInput != null)
                _playerInput.onControlsChanged += OnControlsChanged;
        }

        private void OnDestroy()
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;
        }
        #endregion
        
        #region Main Methods

        public void SetPlayerInput(PlayerInput playerInput)
        {
            Info($"PlayerInput set to {playerInput}");
            _playerInput = playerInput;
            CacheSchemeAndDevices();
        }

        
        public void SwitchToUI()
        {
            _playerInput.SwitchCurrentActionMap("UI");
            _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
        }

        public void SwitchToPlayer()
        {
            _playerInput.SwitchCurrentActionMap("Player");
            _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
        }
        #endregion
        
        #region Utils
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reapply scheme and devices after a scene load
            if (!string.IsNullOrEmpty(_cachedScheme) && _cachedDevices != null && _playerInput != null)
            {
                _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
            }
        }

        private void OnControlsChanged(PlayerInput input)
        {
            CacheSchemeAndDevices();
        }
        
        private void CacheSchemeAndDevices()
        {
            if (_playerInput == null)
            {
                Error($"PlayerInput not found!");
                return;
            }
            _cachedScheme = _playerInput.currentControlScheme;
            _cachedDevices = _playerInput.devices.ToArray();
        } 
        #endregion
    }
}