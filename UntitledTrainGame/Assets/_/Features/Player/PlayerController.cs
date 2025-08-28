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
    [RequireComponent(typeof(Animator))]
    public class PlayerController : FMono
    {
        
        #region Variables
        
        #region Private
        // Private Variables
        
        // References
        [Header("UI References")]
        [SerializeField] private GameObject _interactionPopup;
        // [SerializeField] private float _popupOffsetY = 1f;
        
        [Header("Player Stop")]
        [SerializeField] private EmptyEventChannel _onPlayerJourneyEnd;
        
        // References
        private CharacterController _characterController;
        private Animator _animator;
        private GDControlPanel _controlPanel;
        private Transform _cameraTransform;
        private Camera _camera;
        
        
        private Ray _wallDetectionRay;
        private RaycastHit _detectionHit;
        
        // Movement Variables
        private float _turnSmoothVelocity;
        private Vector2 _inputMove;
        private float _maxMoveSpeed;
        private float _turnSmoothTime;
        private Vector3 direction;
        
       
        //Raycast Command
        [Header("Raycast Command")]
        [SerializeField] private int _numInteractionRaycasts = 5;
        [SerializeField] private int _numObstacleRaycasts = 3;
        [SerializeField] private int _maxRayHits = 4;
        
        [Header("Raycast Masks")]
        [SerializeField] private LayerMask _interactionMask;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private LayerMask _interactableObstacleMask;

        // Interaction Raycast command
        private NativeArray<RaycastCommand> _interactionCommands;
        private NativeArray<RaycastHit> _interactionResults;
        private JobHandle _interactionJob;
        
        // Player Interaction
        private IInteractable _interactable;
        private bool _canInteract;
        // Interaction Variables
        private float _interactionDistance = 3f;
        private float _interactionAngle = 45f;

        // Obstacle Raycast Command
        private NativeArray<RaycastCommand> _obstacleCommands;
        private NativeArray<RaycastHit> _obstacleResults;
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
            // SetupInteractionRaycasts();
            
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
            _animator = GetComponent<Animator>();
            _controlPanel = GDControlPanel.Instance;
            GDControlPanel.OnValuesUpdated += OnControlPanelUpdated;
            GetFromControlPanel();
            
            _cameraTransform = _camera.transform;
            _inputMove = Vector2.zero;

        }

        private void OnDestroy()
        {
            GDControlPanel.OnValuesUpdated -= OnControlPanelUpdated;
        }

        // Update is called once per frame
        void Update()
        {
            var last_pos = transform.position;
            HandleMovement();
            SetupInteractionRaycasts();
            var current_pos = transform.position;
            var velocity = (current_pos - last_pos) / Time.deltaTime;
            _animator.SetFloat("Velocity", velocity.magnitude);
            Info($"Setting velocity in animator: {velocity.magnitude} / {_maxMoveSpeed} = {(velocity.magnitude/ _maxMoveSpeed)}");
            
        }

        private void LateUpdate()
        {
            CompleteInteractionJob();
        }

        #endregion

        #region Main Methods

        #region Input

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
        }
        
        #endregion

        private void HandleMovement()
        {
            var finalMoveSpeed = _maxMoveSpeed;
            
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

            if (Physics.Raycast(_wallDetectionRay, out _detectionHit, 0.5f, _obstacleMask))
            {
                Debug.DrawLine(transform.position, _detectionHit.point, Color.cyan, 0.05f);
                Info($"Finding new direction to glide");
                var wallNormal = _detectionHit.normal;
                direction = Vector3.ProjectOnPlane(direction, wallNormal);
                // direction = Vector3.Lerp(
                //     direction,
                //     Vector3.ProjectOnPlane(direction, wallNormal),
                //     _turnSmoothVelocity * Time.deltaTime);
                // direction.Normalize();
                // direction = newDirection;
                // direction = Vector3.RotateTowards(direction, newDirection, _turnSmoothVelocity * Time.deltaTime,0f);
                Info($"New direction: {direction} : normal {wallNormal}");     
            }

            if (!(direction.magnitude >= 0.1f)) return;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, _turnSmoothTime);
                
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Apply gravity to player
            direction.y = -2f;
            _characterController.Move(direction * (_maxMoveSpeed * Time.deltaTime));
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
                        _canInteract = true;
                    }

                    if(_canInteract && _interactable != null)
                        Info($"[INTERACT] Found {hit.collider.name}");
                }
            }
            
            Info($"Interaction job completed: {_canInteract}");
            ShowInteractionPopup(_canInteract);
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
        
        private void SetupObstacleRaycast()
        {
            Vector3 origin = transform.position;
            var qpObstacle = new QueryParameters
            {
                layerMask = _obstacleMask | _interactableObstacleMask,
                hitTriggers = QueryTriggerInteraction.Ignore,
            };

            // forward, left, right
            Vector3[] obstacleDirs =
            {
                transform.forward,
                Quaternion.Euler(0, -45, 0) * transform.forward,
                Quaternion.Euler(0, 45, 0) * transform.forward
            };
            
            for (int i = 0; i < _numObstacleRaycasts; i++)
            {
                _obstacleCommands[i] = new RaycastCommand(origin, obstacleDirs[i].normalized, qpObstacle, 1f);
            }
            
            _obstacleJob = RaycastCommand.ScheduleBatch(_obstacleCommands, _obstacleResults, 1);
        }
        
        private void OnControlPanelUpdated(GDControlPanel controlPanel)
        {
            GetFromControlPanel();
        }

        private void GetFromControlPanel()
        {
            _maxMoveSpeed = _controlPanel.PlayerMoveSpeed;
            _turnSmoothTime = _controlPanel.TurnSmoothTime;
            _interactionDistance = _controlPanel.DetectionDistance;
            _interactionAngle = _controlPanel.DetectionAngle;
        }

        private void ShowInteractionPopup(bool enable)
        {
            if (_interactionPopup == null) return;
            _interactionPopup.SetActive(enable);
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