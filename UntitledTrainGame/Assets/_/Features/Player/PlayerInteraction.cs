using System;
using Foundation.Runtime;
using Manager.Runtime;
using SharedData.Runtime;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    [RequireComponent(typeof(PlayerInputRouter))]
    public class PlayerInteraction : FMono
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        private GDControlPanel _controlPanel;
        private PlayerInputRouter _inputRouter;

        [Header("UI References - Interaction Bubble")]
        [SerializeField] private GameObject _interactionBubble;
        [SerializeField] private GameObject _interactionBubbleEnter;
        [SerializeField] private GameObject _interactionBubbleTrain;
        [SerializeField] private GameObject _interactionBubbleBench;
        [SerializeField] private GameObject _interactionBubblePickUp;
        [SerializeField] private GameObject _interactionBubbleDialog;
        
        [Header("UI References - Bench Choice")]
        [SerializeField] private GameObject _benchChoice;
        [SerializeField] private GameObject _sleepUnselected;
        [SerializeField] private GameObject _sleepSelected;
        [SerializeField] private GameObject _waitUnselected;
        [SerializeField] private GameObject _waitSelected;
        
        // Bench choice state
        private bool _isBenchChoiceOpen = false;
        // 0 = Sleep, 1 = Wait
        private int _benchSelectedIndex = 0;
        
        
        //Raycast Command
        [Header("Raycast Command")]
        [SerializeField] private int _numInteractionRaycasts = 5;
        [SerializeField] private int _maxRayHits = 4;
        
        [Header("Raycast Masks")]
        [SerializeField] private LayerMask _interactionMask;
        [SerializeField] private LayerMask _interactableObstacleMask;

        // Interaction Raycast command
        private NativeArray<RaycastCommand> _interactionCommands;
        private NativeArray<RaycastHit> _interactionResults;
        private JobHandle _interactionJob;
        
        // Player Interaction
        private IInteractable _interactable;
        private bool _canInteract;
        private InteractionType _interactionType;
        // Interaction Variables
        private float _interactionDistance = 3f;
        private float _interactionAngle = 45f;
        
        
        
        // End of Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        // End of Public Variables
        #endregion
        
        #endregion
        
        #region Unity API
        
        private void OnEnable()
        {
            // Interaction Raycast
            _interactionCommands = new NativeArray<RaycastCommand>(_numInteractionRaycasts, Allocator.Persistent);
            _interactionResults = new NativeArray<RaycastHit>(_numInteractionRaycasts * _maxRayHits, Allocator.Persistent);
            // SetupInteractionRaycasts();
            
        }
        private void OnDisable()
        {
            if(_interactionCommands.IsCreated) _interactionCommands.Dispose();
            if (_interactionResults.IsCreated) _interactionResults.Dispose();
        }
        
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controlPanel = GDControlPanel.Instance;
            _inputRouter = GetComponent<PlayerInputRouter>();
            
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
            _inputRouter.OnInteract += OnInteract;
            GetFromControlPanel(); 
            
            // Ensure bench choice UI is hidden at start and visuals initialized
            if (_benchChoice != null) _benchChoice.SetActive(false);
            _benchSelectedIndex = 0;
            UpdateBenchChoiceVisuals();
        }

        private void OnDestroy()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
            _inputRouter.OnInteract -= OnInteract;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isBenchChoiceOpen)
            {
                HandleBenchChoiceInput();
                return;
            }
            
            // var last_pos = transform.position;
            // HandleMovement();
            SetupInteractionRaycasts(); 
        }
        
        private void LateUpdate()
        {
            CompleteInteractionJob();
        }
        
        #endregion
        
        #region Main Methods

        private void OnInteract()
        {
            // If bench choice is open, confirm selection with E
            if (_isBenchChoiceOpen)
            {
                ConfirmBenchChoice();
                return;
            }

            if (!_canInteract || _interactable == null)
            {
                Error($"Interaction failed.");
                return;
            }
            
            // If interacting with bench, open bench choice UI
            // if (_interactable.InteractionType == InteractionType.Bench)
            // {
            //     OpenBenchChoice();
            //     return;
            // }
            Info($"Interacting with {_interactable.InteractionType}");
            _interactable.Interact();
        }
        
        private void CompleteInteractionJob()
        {
            _interactionJob.Complete();
            
            // While bench choice is open, keep current interactable
            if(_isBenchChoiceOpen) return;
            
            _interactable = null;
            _canInteract = false;

            float angleDegrees = _interactionAngle;
            float facingDotThreshold = Mathf.Cos(Mathf.Deg2Rad * (angleDegrees * 0.5f)); // safer than ad-hoc 0.9
            
            Vector3 origin = transform.position;
            
            float nearestDistance = float.MaxValue;
            for (int ray = 0; ray < _numInteractionRaycasts; ray++)
            {
                float angle = (_interactionAngle / _numInteractionRaycasts) * ray;
                // Info($"Ray angle: {angle}");
                Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
                // Info($"Ray direction: {dir}");

                float dot = Vector3.Dot(transform.forward.normalized, dir.normalized);
                //Info($"[INTERACT] Checking dot, threshold: {facingDotThreshold}, dot: {dot}");
                if (dot < facingDotThreshold) continue;

                int baseIndex = ray * _maxRayHits;
                for (int j = 0; j < _maxRayHits; j++)
                {

                    var hit = _interactionResults[baseIndex + j];
                    if (hit.collider == null) continue;

                    Debug.DrawLine(origin, hit.point, Color.cyan, 0.05f);
                    
                    var interactable = hit.collider.GetComponent<IInteractable>();
                    Info($"[INTERACT] Found interactable: {interactable}]");
                    if (interactable == null) continue;

                    var dist = Vector3.Distance(origin, hit.point);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        _interactable = interactable;
                        _interactionType = _interactable.InteractionType;
                        _canInteract = true;
                    }

                    if(_canInteract && _interactable != null)
                        Info($"[INTERACT] Found {hit.collider.name}");
                }
            }
            
            Info($"Interaction job completed: {_canInteract}");
            ShowInteractionPopup(_canInteract, _interactionType);
        }
        
        private void ShowInteractionPopup(bool enable, InteractionType type)
        {
            Assert.IsNotNull(_interactionBubble);
            Assert.IsNotNull(_interactionBubbleTrain);
            Assert.IsNotNull(_interactionBubbleDialog);
            Assert.IsNotNull(_interactionBubbleBench);
            Assert.IsNotNull(_interactionBubbleEnter);
            Assert.IsNotNull(_interactionBubblePickUp);


            if (_interactionBubble == null) return;
            switch (type)
            {
                case InteractionType.Train:
                    _interactionBubble.SetActive(enable);
                    _interactionBubbleTrain.SetActive(enable);
                    break;
                case InteractionType.Dialog:
                case InteractionType.Inspect:
                case InteractionType.Read:
                    _interactionBubble.SetActive(enable);
                    _interactionBubbleDialog.SetActive(enable);
                    break;
                case InteractionType.Bench:
                    _interactionBubble.SetActive(enable /*&& !_isBenchChoiceOpen*/);
                    _interactionBubbleBench.SetActive(enable /*&& !_isBenchChoiceOpen*/);
                    break;
                case InteractionType.EnterBuilding:
                    _interactionBubble.SetActive(enable);
                    _interactionBubbleEnter.SetActive(enable);
                    break;
                case InteractionType.PickUp:
                    _interactionBubble.SetActive(enable);
                    _interactionBubblePickUp.SetActive(enable);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            _interactionBubble.SetActive(enable);
            
        }
        
        private void OpenBenchChoice()
        {
            if (_isBenchChoiceOpen) return;
            //store interactable bench
            _interactionBubble.SetActive(false);
            _benchSelectedIndex = 0;
            UpdateBenchChoiceVisuals();
             
            _benchChoice.SetActive(true);
             
            CustomInputManager.Instance.SwitchToUI();
             
            _isBenchChoiceOpen = true;
        }
        private void CloseBenchChoice()
        {
            if (!_isBenchChoiceOpen) return;
            _benchChoice.SetActive(false);
            
            CustomInputManager.Instance.SwitchToPlayer();

            _isBenchChoiceOpen = false;
        }

        private void UpdateBenchChoiceVisuals()
        {
            bool sleepSelected = _benchSelectedIndex == 0;
            if(_sleepSelected != null) _sleepSelected.SetActive(sleepSelected);
            if(_sleepUnselected != null) _sleepUnselected.SetActive(!sleepSelected);
            
            bool waitSelected = _benchSelectedIndex == 1;
            if(_waitSelected != null) _waitSelected.SetActive(waitSelected);
            if(_waitUnselected != null) _waitUnselected.SetActive(!waitSelected);
        }
        
        private void HandleBenchChoiceInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            
            // Toogle selection on W or S
            if (kb.wKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            {
                _benchSelectedIndex = _benchSelectedIndex == 0 ? 1 : 0;
                UpdateBenchChoiceVisuals();
            }
            
            // Confirm with E
            {
                ConfirmBenchChoice();
            }
        }
        private void ConfirmBenchChoice()
        {
            if (_benchChoice == null) return;
            var interactable = _benchChoice.transform.GetChild(_benchSelectedIndex).GetComponent<IInteractable>();
            if (interactable == null) return;
            interactable.Interact();
            CloseBenchChoice();
        }
        
        #endregion
        

        #region Utils
        
        private void SetupInteractionRaycasts()
        {
            Vector3 origin = transform.position;

            var qpInteract = new QueryParameters
            {
                layerMask = _interactionMask | _interactableObstacleMask,
                hitTriggers = QueryTriggerInteraction.Ignore,
            };

            for (int i = 0; i < _numInteractionRaycasts; i++)
            {
                float angle = (_interactionAngle / _numInteractionRaycasts) * i;
                
                Vector3 dir = Quaternion.Euler(0,angle,0) * transform.forward;
                // use layer because maybe raycast is getting filled
                _interactionCommands[i] = new RaycastCommand(origin, dir, qpInteract, _interactionDistance);
            }
            
            _interactionJob = RaycastCommand.ScheduleBatch(_interactionCommands, _interactionResults, 1);
        }
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            GetFromControlPanel();
        }
        private void GetFromControlPanel()
        {
            _interactionDistance = _controlPanel.DetectionDistance;
            _interactionAngle = _controlPanel.DetectionAngle;
        }
        
        #endregion
        
        #region Debug
        // Debug
        private void OnDrawGizmosSelected()
        {
            // Visualize view radius
            // Gizmos.color = Color.yellow;
            // Gizmos.DrawWireSphere(transform.position, _interactionDistance);

            // Visualize view angle
            Vector3 viewAngleA = DirFromAngle(-_interactionAngle / 2);
            Vector3 viewAngleB = DirFromAngle(_interactionAngle / 2);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + viewAngleA * _interactionDistance);
            Gizmos.DrawLine(transform.position, transform.position + viewAngleB * _interactionDistance);
        }
        
        private Vector3 DirFromAngle(float angleInDegrees)
        {
            /*
                - represents the object's rotation around the vertical axis (`Y-axis`) in degrees. `transform.eulerAngles.y`
                - The input is adjusted by adding the current rotation of the object to it.
                This ensures the angle is relative to the direction the object is facing, instead of being fixed globally. `angleInDegrees`
                This creates a direction vector in the X-Z plane pointing in the direction of the given angle relative to the object's rotation.

             */
            angleInDegrees += transform.eulerAngles.y;
            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }
        #endregion
        
    }
}