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
        private string _cachedActionMap; // "Player" or "UI"
        
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
            // CacheSchemeAndDevices();
            // Subscribe then resolve PlayerInput present in the scene
            UnitySceneManager.sceneLoaded += OnSceneLoaded;
            
            // Try to resolve a PlayerInput in the scene
            EnsurePlayerInputBound();
            
            // Now that we have (or tried to get) a PlayerInput, cache what we can
            CacheOrInferSchemeAndDevices();
            CacheCurrentActionMap();

            // _playerInput = FindAnyObjectByType<PlayerInput>();
            // if (_playerInput != null)
            //     _playerInput.onControlsChanged += OnControlsChanged;
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
            if (_playerInput == playerInput) return;
            
            // unsubscribe from previous PlayerInput
            if (_playerInput != null)
                _playerInput.onControlsChanged -= OnControlsChanged;
            
            _playerInput = playerInput;
            
            // subscribe to new PlayerInput
            if(_playerInput != null)
                _playerInput.onControlsChanged += OnControlsChanged;

            CacheOrInferSchemeAndDevices();
            CacheCurrentActionMap();
            // CacheSchemeAndDevices();
        }

        
        public void SwitchToUI()
        {
            EnsurePlayerInputBound();
            if(_playerInput == null) return;

            _cachedActionMap = "UI";
            _playerInput.SwitchCurrentActionMap("UI");
            
            // Try to reapply scheme/devices if we have them, otherwise they'll be captured on next controlsChanged
            ReapplySchemeAndDevicesIfCached();

            // _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
        }

        public void SwitchToPlayer()
        {
            EnsurePlayerInputBound();
            if(_playerInput == null) return;
            
            _cachedActionMap = "Player";
            _playerInput.SwitchCurrentActionMap("Player");
            
            // Try to reapply scheme/devices if we have them, otherwise they'll be captured on next controlsChanged
            ReapplySchemeAndDevicesIfCached();
            // _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
        }
        #endregion
        
        #region Utils
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsurePlayerInputBound();
            
            // Reapply scheme and devices after a scene load
            // if (!string.IsNullOrEmpty(_cachedScheme) && _cachedDevices != null && _playerInput != null)
            // {
            //     _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
            // }
            
            // Reapply cached action map (defaults to Player if unknown)
            // if (string.IsNullOrEmpty(_cachedActionMap))
            //     _cachedActionMap = "Player";
            // if (_playerInput != null && _playerInput.currentActionMap?.name != _cachedActionMap)
            // {
            //     _playerInput.SwitchCurrentActionMap(_cachedActionMap);
            // }
            //
            // // Reapply scheme and devices if we have them, or try to infer again if we don't
            // if (!ReapplySchemeAndDevicesIfCached())
            // {
            //     CacheOrInferSchemeAndDevices();
            //     ReapplySchemeAndDevicesIfCached();
            // }

            StartCoroutine(ReapplyInputNextFrame());

        }
        
        private System.Collections.IEnumerator ReapplyInputNextFrame()
        {
            // Wait one frame to ensure PlayerInput is enabled and devices/users are initialized
            yield return null;

            EnsurePlayerInputBound();
            if (_playerInput == null) yield break;

            // Restore action map (default to Player)
            if (string.IsNullOrEmpty(_cachedActionMap))
                _cachedActionMap = "Player";
            if (_playerInput.currentActionMap == null || _playerInput.currentActionMap.name != _cachedActionMap)
                _playerInput.SwitchCurrentActionMap(_cachedActionMap);

            // Try immediate scheme reapply; if user is invalid, keep trying until it becomes valid
            yield return StartCoroutine(EnsureUserValidThenReapplyScheme());
        }
        private System.Collections.IEnumerator EnsureUserValidThenReapplyScheme()
        {
            EnsurePlayerInputBound();
            if (_playerInput == null) yield break;

            // Wait up to a short timeout for the user to become valid (devices paired)
            const float timeout = 2f;
            float t = 0f;
            while (_playerInput != null && !_playerInput.user.valid && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            // Refresh scheme/devices now that user may be valid
            CacheOrInferSchemeAndDevices();
            ReapplySchemeAndDevicesIfCached();
        }


        private void OnControlsChanged(PlayerInput input)
        {
            // CacheSchemeAndDevices();
            CacheOrInferSchemeAndDevices();
        }
        
        private void EnsurePlayerInputBound()
        {
            if (_playerInput != null) return;

            var found = FindAnyObjectByType<PlayerInput>();
            if (found == null)
            {
                Warning("No PlayerInput found in scene yet. Will retry on next opportunity.");
                return;
            }

            SetPlayerInput(found);
        }

        private void CacheCurrentActionMap()
        {
            if (_playerInput != null && _playerInput.currentActionMap != null)
            {
                _cachedActionMap = _playerInput.currentActionMap.name;
            }
            else if (string.IsNullOrEmpty(_cachedActionMap))
            {
                _cachedActionMap = "Player";
            }
        }

        
        private void CacheOrInferSchemeAndDevices()
        {
            if (_playerInput == null)
            {
                Error("PlayerInput not found!");
                return;
            }

            // Try to read current scheme/devices; these can be null/empty during scene loads
            // var scheme = _playerInput.currentControlScheme;
            // var devices = _playerInput.devices;
            //
            // if (!string.IsNullOrEmpty(scheme) && devices is { Count: > 0})
            // {
            //     _cachedScheme = scheme;
            //     _cachedDevices = devices.ToArray();
            //     Info($"Cached scheme '{_cachedScheme}' with {_cachedDevices.Length} device(s).");
            //     return;
            // }
            //
            // // Attempt to infer from PlayerInput default or from paired devices
            // if (string.IsNullOrEmpty(_cachedScheme))
            // {
            //     // Use defaultControlScheme if provided, else keep last known or fallback to "Keyboard&Mouse"/"Gamepad" heuristic if needed
            //     if (!string.IsNullOrEmpty(_playerInput.defaultControlScheme))
            //         _cachedScheme = _playerInput.defaultControlScheme;
            //     else if (!string.IsNullOrEmpty(scheme))
            //         _cachedScheme = scheme;
            //     else if (Keyboard.current != null && Mouse.current != null)
            //         _cachedScheme = "Keyboard&Mouse";
            //     else if (Gamepad.current != null)
            //         _cachedScheme = "Gamepad";
            //     else
            //         _cachedScheme = scheme; // may be null until controls resolve; okay, we’ll update on OnControlsChanged
            // }
            
            // Scheme may be null mid-load; keep last known or default
            var scheme = _playerInput.currentControlScheme;
            if (!string.IsNullOrEmpty(scheme))
                _cachedScheme = scheme;
            else if (string.IsNullOrEmpty(_cachedScheme))
                _cachedScheme = string.IsNullOrEmpty(_playerInput.defaultControlScheme) ? "Keyboard&Mouse" : _playerInput.defaultControlScheme;


            // Infer devices from paired devices if available
            if (_playerInput.user.valid)
            {
                var paired = _playerInput.user.pairedDevices;
                _cachedDevices = paired is { Count: > 0 } ? paired.ToArray() : System.Array.Empty<InputDevice>();
            }
            else _cachedDevices = System.Array.Empty<InputDevice>();

            // // As a last resort, build devices array from common devices
            // if (_cachedDevices == null || _cachedDevices.Length == 0)
            // {
            //     if (_cachedScheme != null && _cachedScheme.Contains("Keyboard") && Keyboard.current != null && Mouse.current != null)
            //     {
            //         _cachedDevices = new InputDevice[] { Keyboard.current, Mouse.current };
            //     }
            //     else if (_cachedScheme != null && _cachedScheme.Contains("Gamepad") && Gamepad.current != null)
            //     {
            //         _cachedDevices = new InputDevice[] { Gamepad.current };
            //     }
            //     else
            //     {
            //         // Leave devices null/empty; when Input auto-detects and raises onControlsChanged, we’ll cache real ones.
            //         _cachedDevices = System.Array.Empty<InputDevice>();
            //     }
            // }

            Info($"Inferred scheme '{_cachedScheme}' with {_cachedDevices.Length} device(s) (user.valid={_playerInput.user.valid}).");
        }
        
        private bool ReapplySchemeAndDevicesIfCached()
        {
            if (_playerInput == null) return false;

            // If we don't have a cached scheme, there's nothing to reapply yet
            if (string.IsNullOrEmpty(_cachedScheme))
                return false;

            if (!_playerInput.user.valid)
            {
                Warning("PlayerInput user invalid; skipping scheme reapply until valid.");
                return false;
            }

            // If devices list is empty, try switching scheme without devices; Input System may bind by itself
            if (_cachedDevices == null || _cachedDevices.Length == 0)
            {
                try
                {
                    _playerInput.SwitchCurrentControlScheme(_cachedScheme);
                    return true;
                }
                catch(System.Exception ex)
                {
                    Warning($"Failed to reapply scheme '{_cachedScheme}' without devices: {ex.Message}");
                    // Some versions may require devices; will re-cache on next onControlsChanged
                    return false;
                }
            }

            _playerInput.SwitchCurrentControlScheme(_cachedScheme, _cachedDevices);
            return true;
        }


        #endregion
    }
}