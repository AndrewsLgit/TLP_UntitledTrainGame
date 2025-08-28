using System;
using Foundation.Runtime;
using SharedData.Runtime;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Input))]
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerController : FMono
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        [Header("UI References")]
        [SerializeField] private GameObject _interactionPopup;
        [SerializeField] private float _popupOffsetY = 1f;
        
        [Header("Player Stop")]
        [SerializeField] private UnityEvent _onPlayerStop;

        [SerializeField] private EmptyEventChannel _onPlayerJourneyEnd;
        
        private CharacterController _characterController;
        private GDControlPanel _controlPanel;
        private Transform _cameraTransform;
        private Camera _camera;
        
        // Player Interaction
        private IInteractable _interactable;
        private SphereCollider _detectionCollider;
        private bool _canInteract;
        
        private Ray _wallDetectionRay;
        private RaycastHit _detectionHit;
        
        // Movement Variables
        private float _turnSmoothVelocity;
        private Vector2 _inputMove;
        private float _moveSpeed;
        private float _turnSmoothTime;
        private Vector3 direction;
        
        // Interaction Variables
        private float _interactionDistance = 5f;
        private float _interactionAngle = 45f;
        private Vector3 _directionToInteractable;
        
        //Raycast Command
        [Header("Raycast Command")]
        [SerializeField] private float _scanDistance = 3f;

        [SerializeField] private int _numInteractionRaycasts = 3;
        [SerializeField] private int _numObstacleRaycasts = 3;
        [SerializeField] private int _maxRayHits = 4;
        
        [Header("Raycast Masks")]
        [SerializeField] private LayerMask _interactionMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private LayerMask _interactableObstacleMask;

        private NativeArray<RaycastCommand> _interactionCommands;
        private NativeArray<RaycastHit> _interactionResults;
        private NativeArray<RaycastCommand> _obstacleCommands;
        private NativeArray<RaycastHit> _obstacleResults;
        
        private JobHandle _interactionJob;
        private JobHandle _obstacleJob;

        // Private Variables
        #endregion
        
        #region Public
        // Public Variables
        
        
        
        // Public Variables
        #endregion
        
        #endregion
        
        #region Unity API

        private void Awake()
        {
            SetFact("playerTransform", transform, false);
        }
        
        private void OnEnable()
        {
            // Interaction Raycast
            _interactionCommands = new NativeArray<RaycastCommand>(_numInteractionRaycasts, Allocator.Persistent);
            _interactionResults = new NativeArray<RaycastHit>(_numInteractionRaycasts * _maxRayHits, Allocator.Persistent);
            SetupInteractionRaycasts();
            
            // Wall Raycast
            _obstacleCommands = new NativeArray<RaycastCommand>(_numObstacleRaycasts, Allocator.Persistent);
            _obstacleResults = new NativeArray<RaycastHit>(_numObstacleRaycasts * _maxRayHits, Allocator.Persistent);
        }
        private void OnDisable()
        {
            if(_interactionCommands.IsCreated) _interactionCommands.Dispose();
            if (_interactionResults.IsCreated) _interactionResults.Dispose();
            
            if (_obstacleCommands.IsCreated) _obstacleCommands.Dispose();
            if (_obstacleResults.IsCreated) _obstacleResults.Dispose();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _camera = Camera.main;
            _characterController = GetComponent<CharacterController>();
            _detectionCollider = GetComponent<SphereCollider>();
            _controlPanel = GDControlPanel.Instance;
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
            GetFromControlPanel();
            
            _cameraTransform = _camera.transform;
            _inputMove = Vector2.zero;

            if (_detectionCollider != null)
            {
                _detectionCollider.radius = _interactionDistance;
                _detectionCollider.isTrigger = true;
                Info($"Detection collider configured: radius {_detectionCollider.radius}, isTrigger: {_detectionCollider.isTrigger}");
            }
            else Error("No detection collider found!");
            
        }

        private void OnDestroy()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
        }

        

        // Update is called once per frame
        void Update()
        {
            HandleMovement();
            // InteractionDetection();
            
             SetupInteractionRaycasts();
            //_interactionJob = RaycastCommand.ScheduleBatch(_interactionCommands, _interactionResults, 1);
        }

        private void LateUpdate()
        {
            CompleteInteractionJob();
        }

        // private void OnTriggerEnter(Collider other)
        // {
        //     if (other.transform == transform || other.transform.IsChildOf(transform)) return;
        //     Info($"Entering trigger with {other.gameObject.name}");
        //
        //     if (other.TryGetComponent(out IInteractable interactable))
        //     {
        //         Info("Found interactable near player");
        //         _interactable = interactable;
        //     }
        //     else
        //     {
        //         Info($"No interactable found on {other.gameObject.name}");
        //     }
        //     //var interactable = other.GetComponent<IInteractable>();
        //     // if (interactable == null) return;
        //     // _interactable = interactable;
        //     // Info($"Passed interactable: {interactable.GetType()} to player");
        //     // _canInteract = true;
        // }
        // private void OnTriggerExit(Collider other)
        // {
        //     if(other.transform == transform || other.transform.IsChildOf(transform)) return;
        //     
        //     Info($"Exiting trigger");
        //     // var interactable = other.GetComponent<IInteractable>();
        //     // if (interactable == null || interactable != _interactable) return;
        //     _interactable = null;
        //     _canInteract = false;
        //     ShowInteractionPopup(false);
        //     // _canInteract = false;
        // }
        #endregion

        #region Main Methods

        public void Move(InputAction.CallbackContext context)
        {
            _inputMove = context.ReadValue<Vector2>();
        }
        
        public void InteractWith(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Canceled) return;
            if (!_canInteract || _interactable == null) return;
            _interactable.Interact();
        }
        
        public void StopTrain(InputAction.CallbackContext context)
        {
            if (context.phase != InputActionPhase.Canceled) return;
            _onPlayerJourneyEnd?.Invoke();
            // _onPlayerStop.Invoke();
        }

        private void HandleMovement()
        {
            var finalMoveSpeed = _moveSpeed;
            
            
            if (_camera == null)
            {
                _camera = Camera.main;
                _cameraTransform = _camera.transform;
            }
            // var speed = _controlPanel.PlayerMoveSpeed;
            // var turnSmoothTime = _controlPanel.TurnSmoothTime;
            var direction = new Vector3(_inputMove.x, 0f, _inputMove.y).normalized;
            
            var forward = _cameraTransform.forward;
            var right = _cameraTransform.right;
            
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            direction = right * _inputMove.x + forward * _inputMove.y;
            direction = direction.normalized;
            
            _wallDetectionRay = new Ray(transform.position, direction);

            if (Physics.Raycast(_wallDetectionRay, out _detectionHit, _interactionDistance, _obstacleMask))
            {
                Info($"Finding new direction to glide");
                var wallNormal = _detectionHit.normal;
                direction = Vector3.ProjectOnPlane(direction, wallNormal);
                Info($"New direction: {direction} : normal {wallNormal}");     
            }
            
            

            if (!(direction.magnitude >= 0.1f)) return;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Apply gravity to player
            direction.y = -2f;
            _characterController.Move(direction * (_moveSpeed * Time.deltaTime));
        }

        private void InteractionDetection()
        {
            if (_interactable == null)
            {
                _canInteract = false;
                ShowInteractionPopup(false);
                return;
            }
            
            var interactablePosition = ((MonoBehaviour)_interactable).transform.position;
            _directionToInteractable = interactablePosition - transform.position;
            // calculate angle between player's forward and the interactable gameObject
            float angle = Vector3.Angle(transform.forward, _directionToInteractable);
            
            bool wasInteracting = _canInteract;
            _canInteract = angle <= _interactionAngle;
            
            if(_canInteract != wasInteracting) ShowInteractionPopup(_canInteract);
            //if (_canInteract && _interactionPopup.activeInHierarchy) UpdatePopupPosition();
            
            if (_canInteract) Info($"Found interactable: Dir({_directionToInteractable}) Angle({angle})");
        }

        private void SetupInteractionRaycasts()
        {
            Vector3 origin = transform.position;

            var qpInteract = new QueryParameters
            {
                layerMask = _interactionMask ,
                hitTriggers = QueryTriggerInteraction.Ignore,
            };

            for (int i = 0; i < _numInteractionRaycasts; i++)
            {
                float angle = (_interactionAngle / _numInteractionRaycasts) * i;
                // float angle = (i / (float)(_numInteractionRaycasts - 1)) * 360f;
                
                Vector3 dir = Quaternion.Euler(0,angle,0) * transform.forward;
                // use layer because maybe raycast is getting filled
                _interactionCommands[i] = new RaycastCommand(origin, dir, qpInteract, _interactionDistance);
            }
            
            _interactionJob = RaycastCommand.ScheduleBatch(_interactionCommands, _interactionResults, 1);
        }

        private void CompleteInteractionJob()
        {
            _interactionJob.Complete();
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
                // use layer because maybe raycast is getting filled

                float dot = Vector3.Dot(transform.forward.normalized, dir.normalized);
                //Info($"[INTERACT] Checking dot, threshold: {facingDotThreshold}, dot: {dot}");
                if (dot < facingDotThreshold) continue;

                int baseIndex = ray * _maxRayHits;
                for (int j = 0; j < _maxRayHits; j++)
                {

                    var hit = _interactionResults[baseIndex + j];
                    //Info($"[INTERACT] Raycast hit: {hit}");
                    if (hit.collider == null) continue;
                    Debug.DrawLine(origin, hit.point, Color.red, 0.05f);

                    var interactable = hit.collider.GetComponent<IInteractable>();
                    Info($"[INTERACT] Found interactable: {interactable}]");
                    if (interactable == null) continue;

                    var dist = Vector3.Distance(origin, hit.point);
                    if (dist < nearestDistance)
                    {
                        nearestDistance = dist;
                        _interactable = interactable;
                        _canInteract = true;
                    }

                    //_canInteract = true;
                    if(_canInteract && _interactable != null)
                        Info($"[INTERACT] Found {hit.collider.name}");
                    // ShowInteractionPopup(_canInteract);


                    // _interactionResults[i].collider.TryGetComponent(out _interactable);

                    //if (_interactable == null) continue;
                    //_canInteract = (_interactable != null);
                }
            }

            // _canInteract = false;
            Info($"Interaction job completed: {_canInteract}");
            ShowInteractionPopup(_canInteract);
        }
        private void HandleWallRaycast()
        {
            Vector3 origin = transform.position;

            // forward, left, right
            Vector3[] wallDirs =
            {
                transform.forward,
                Quaternion.Euler(0, -45, 0) * transform.forward,
                Quaternion.Euler(0, 45, 0) * transform.forward
            };
            
            for (int i = 0; i < _numObstacleRaycasts; i++)
            {
                _obstacleCommands[i] = new RaycastCommand(origin, wallDirs[i].normalized, 2f, _obstacleMask, _maxRayHits);
            }
            
            _obstacleJob = RaycastCommand.ScheduleBatch(_obstacleCommands, _obstacleResults, 1);
        }

        #endregion
        
        #region Utils
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            GetFromControlPanel();
        }

        private void GetFromControlPanel()
        {
            _moveSpeed = _controlPanel.PlayerMoveSpeed;
            _turnSmoothTime = _controlPanel.TurnSmoothTime;
            _interactionDistance = _controlPanel.DetectionDistance;
            _interactionAngle = _controlPanel.DetectionAngle;
        }

        private void ShowInteractionPopup(bool enable)
        {
            if (_interactionPopup == null) return;
            _interactionPopup.SetActive(enable);
            //if (enable) UpdatePopupPosition();
        }

        private void UpdatePopupPosition()
        {
            if(_interactionPopup == null || _camera == null) return;

            Vector3 playerScreenPos = _camera.WorldToScreenPoint(transform.position);
            
            var dirToCamera = (_camera.transform.position - transform.position).normalized;
            var popupPos = playerScreenPos + dirToCamera * .5f;

            // playerScreenPos.y += _popupOffsetY;
            _interactionPopup.transform.position = playerScreenPos;
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